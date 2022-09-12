using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SubtitleExtract.Tasks;

/// <summary>
/// Scheduled task to extract embedded subtitles for immediate access in web player.
/// </summary>
public class ExtractSubtitlesTask : IScheduledTask
{
    private const int QueryPageLimit = 100;

    private readonly ILibraryManager _libraryManager;
    private readonly ISubtitleEncoder _subtitleEncoder;
    private readonly ILocalizationManager _localization;
    private readonly ILogger<ExtractSubtitlesTask> _logger;

    private static readonly BaseItemKind[] _itemTypes = { BaseItemKind.Episode, BaseItemKind.Movie };
    private static readonly string[] _supportedFormats = { SubtitleFormat.SRT, SubtitleFormat.ASS, SubtitleFormat.SSA };
    private static readonly string[] _mediaTypes = { MediaType.Video };
    private static readonly SourceType[] _sourceTypes = { SourceType.Library };
    private static readonly DtoOptions _dtoOptions = new(false);

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtractSubtitlesTask" /> class.
    /// </summary>
    /// <param name="libraryManager">Instance of <see cref="ILibraryManager"/> interface.</param>
    /// /// <param name="subtitleEncoder">Instance of <see cref="ISubtitleEncoder"/> interface.</param>
    /// <param name="localization">Instance of <see cref="ILocalizationManager"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{ExtractSubtitlesTask}"/> interface.</param>
    public ExtractSubtitlesTask(
        ILibraryManager libraryManager,
        ISubtitleEncoder subtitleEncoder,
        ILocalizationManager localization,
        ILogger<ExtractSubtitlesTask> logger)
    {
        _libraryManager = libraryManager;
        _subtitleEncoder = subtitleEncoder;
        _localization = localization;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Key => "ExtractSubtitles";

    /// <inheritdoc />
    public string Name => SubtitleExtractPlugin.Current.Name;

    /// <inheritdoc />
    public string Description => SubtitleExtractPlugin.Current.Name;

    /// <inheritdoc />
    public string Category => _localization.GetLocalizedString("TasksLibraryCategory");

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => Enumerable.Empty<TaskTriggerInfo>();

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var query = new InternalItemsQuery
        {
            Recursive = true,
            HasSubtitles = true,
            IsVirtualItem = false,
            IncludeItemTypes = _itemTypes,
            DtoOptions = _dtoOptions,
            MediaTypes = _mediaTypes,
            SourceTypes = _sourceTypes,
            Limit = QueryPageLimit,
        };

        var numberOfVideos = _libraryManager.GetCount(query);

        var startIndex = 0;
        var completedVideos = 0;

        while (startIndex < numberOfVideos)
        {
            query.StartIndex = startIndex;
            var videos = _libraryManager.GetItemList(query);

            foreach (var video in videos)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var mediaSourceId = video.Id.ToString("N", CultureInfo.InvariantCulture);
                    var streams = video.GetMediaStreams()
                        .Where(stream => stream.IsTextSubtitleStream
                                         && stream.SupportsExternalStream
                                         && !stream.IsExternal);
                    foreach (var stream in streams)
                    {
                        var index = stream.Index;
                        var format = stream.Codec;

                        try
                        {
                            // SubtitleEncoder has readers only for these formats, everything else converted to SRT.
                            if (!_supportedFormats.Contains(format, StringComparison.OrdinalIgnoreCase))
                            {
                                format = SubtitleFormat.SRT;
                            }

                            await _subtitleEncoder.GetSubtitles(video, mediaSourceId, index, format, 0, 0, false, cancellationToken).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(
                                ex,
                                "Unable to extract subtitle File:{File}\tStreamIndex:{Index}\tCodec:{Codec}",
                                video.Path,
                                index,
                                format);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Unable to get streams for File:{File}", video.Path);
                }

                completedVideos++;
                progress.Report(100d * completedVideos / numberOfVideos);
            }

            startIndex += QueryPageLimit;
        }

        progress.Report(100);
    }
}
