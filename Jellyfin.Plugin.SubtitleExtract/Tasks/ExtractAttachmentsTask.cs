using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Tasks;

namespace Jellyfin.Plugin.SubtitleExtract.Tasks;

/// <summary>
/// Scheduled task to extract embedded attachments for immediate access in web player.
/// </summary>
public class ExtractAttachmentsTask : IScheduledTask
{
    private const int QueryPageLimit = 250;

    private readonly ILibraryManager _libraryManager;
    private readonly ILocalizationManager _localization;
    private readonly IAttachmentExtractor _extractor;

    private static readonly BaseItemKind[] _itemTypes = [BaseItemKind.Episode, BaseItemKind.Movie];
    private static readonly MediaType[] _mediaTypes = [MediaType.Video];
    private static readonly SourceType[] _sourceTypes = [SourceType.Library];
    private static readonly DtoOptions _dtoOptions = new(false);

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtractAttachmentsTask" /> class.
    /// </summary>
    /// <param name="libraryManager">Instance of <see cref="ILibraryManager"/> interface.</param>
    /// <param name="attachmentExtractor"><see cref="IAttachmentExtractor"/> instance.</param>
    /// <param name="localization">Instance of <see cref="ILocalizationManager"/> interface.</param>
    public ExtractAttachmentsTask(
        ILibraryManager libraryManager,
        IAttachmentExtractor attachmentExtractor,
        ILocalizationManager localization)
    {
        _libraryManager = libraryManager;
        _localization = localization;
        _extractor = attachmentExtractor;
    }

    /// <inheritdoc />
    public string Key => "ExtractAttachments";

    /// <inheritdoc />
    public string Name => "Extract Attachments";

    /// <inheritdoc />
    public string Description => "Extracts embedded attachments.";

    /// <inheritdoc />
    public string Category => _localization.GetLocalizedString("TasksLibraryCategory");

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return [];
    }

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

                foreach (var mediaSource in video.GetMediaSources(false))
                {
                    var streams = mediaSource.MediaStreams.Where(i => i.Type == MediaStreamType.Subtitle).ToList();
                    var mksStreams = streams.Where(i => !string.IsNullOrEmpty(i.Path) && i.Path.EndsWith(".mks", StringComparison.OrdinalIgnoreCase)).ToList();
                    var mksPaths = mksStreams.Select(i => i.Path).ToList();
                    if (mksPaths.Count > 0)
                    {
                        foreach (var path in mksPaths)
                        {
                            await _extractor.ExtractAllAttachments(path, mediaSource, cancellationToken).ConfigureAwait(false);
                        }
                    }

                    if (streams.Count != mksStreams.Count)
                    {
                        await _extractor.ExtractAllAttachments(mediaSource.Path, mediaSource, cancellationToken).ConfigureAwait(false);
                    }
                }

                completedVideos++;
                progress.Report(100d * completedVideos / numberOfVideos);
            }

            startIndex += QueryPageLimit;
        }

        progress.Report(100);
    }
}
