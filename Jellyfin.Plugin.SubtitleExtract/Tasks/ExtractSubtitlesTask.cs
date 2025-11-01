using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Tasks;

namespace Jellyfin.Plugin.SubtitleExtract.Tasks;

/// <summary>
/// Scheduled task to extract embedded subtitles for immediate access in web player.
/// </summary>
public class ExtractSubtitlesTask : IScheduledTask
{
    private const int QueryPageLimit = 250;

    private readonly ILibraryManager _libraryManager;
    private readonly ILocalizationManager _localization;
    private readonly ISubtitleEncoder _encoder;

    private static readonly BaseItemKind[] _itemTypes = [BaseItemKind.Episode, BaseItemKind.Movie];
    private static readonly MediaType[] _mediaTypes = [MediaType.Video];
    private static readonly SourceType[] _sourceTypes = [SourceType.Library];
    private static readonly DtoOptions _dtoOptions = new(false);

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtractSubtitlesTask" /> class.
    /// </summary>
    /// <param name="libraryManager">Instance of <see cref="ILibraryManager"/> interface.</param>
    /// <param name="subtitleEncoder"><see cref="ISubtitleEncoder"/> instance.</param>
    /// <param name="localization">Instance of <see cref="ILocalizationManager"/> interface.</param>
    public ExtractSubtitlesTask(
        ILibraryManager libraryManager,
        ISubtitleEncoder subtitleEncoder,
        ILocalizationManager localization)
    {
        _libraryManager = libraryManager;
        _localization = localization;
        _encoder = subtitleEncoder;
    }

    /// <inheritdoc />
    public string Key => "ExtractSubtitles";

    /// <inheritdoc />
    public string Name => "Extract Subtitles";

    /// <inheritdoc />
    public string Description => "Extracts embedded subtitles.";

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
        var startProgress = 0d;

        var config = SubtitleExtractPlugin.Current.Configuration;
        var libs = config.SelectedSubtitlesLibraries;

        Guid[] parentIds = [];
        if (libs.Length > 0)
        {
            // Try to get parent ids from the selected libraries
            parentIds = _libraryManager.GetVirtualFolders()
                .Where(vf => libs.Contains(vf.Name))
                .Select(vf => Guid.Parse(vf.ItemId))
                .ToArray();
        }

        if (parentIds.Length > 0)
        {
            // In case parent ids are found, run the extraction on each found library
            foreach (var parentId in parentIds)
            {
                startProgress = await RunExtractionWithProgress(progress, parentId, parentIds, startProgress, cancellationToken).ConfigureAwait(false);
            }
        }
        else
        {
            // Otherwise run it on everything
            await RunExtractionWithProgress(progress, null, [], startProgress, cancellationToken).ConfigureAwait(false);
        }

        progress.Report(100);
    }

    private async Task<double> RunExtractionWithProgress(
        IProgress<double> progress,
        Guid? parentId,
        IReadOnlyCollection<Guid> parentIds,
        double startProgress,
        CancellationToken cancellationToken)
    {
        var libsCount = parentIds.Count > 0 ? parentIds.Count : 1;

        var query = new InternalItemsQuery
        {
            Recursive = true,
            HasSubtitles = true,
            IsVirtualItem = false,
            IncludeItemTypes = _itemTypes,
            DtoOptions = _dtoOptions,
            MediaTypes = _mediaTypes,
            SourceTypes = _sourceTypes,
            Limit = QueryPageLimit
        };

        var config = SubtitleExtractPlugin.Current.Configuration;
        // Values are stored separated by comma, and we only need the part before the dash as it is the codec's name.
        string[] selectedCodecs = config.SelectedCodecs;

        var isAdvancedCodecSelection = config.IsAdvancedMode;
        var includeTextSubtitles = config.IncludeTextSubtitles;
        var includeGraphicalSubtitles = config.IncludeGraphicalSubtitles;

        if (!parentId.IsNullOrEmpty())
        {
            query.ParentId = parentId.Value;
        }

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

                foreach (var mediaSource in video.GetMediaSources(false).Where(source => FilterMediasWithCodec(isAdvancedCodecSelection, includeTextSubtitles, includeGraphicalSubtitles, selectedCodecs, source)))
                {
                    await _encoder.ExtractAllExtractableSubtitles(mediaSource, cancellationToken).ConfigureAwait(false);
                }

                completedVideos++;

                // Report the progress using "startProgress" that allows to track progress across multiple libraries
                progress.Report(startProgress + (100d * completedVideos / numberOfVideos / libsCount));
            }

            startIndex += QueryPageLimit;
        }

        // When done, update the startProgress to the current progress for next libraries
        startProgress += 100d * completedVideos / numberOfVideos / libsCount;
        return startProgress;
    }

    /// <summary>
    /// Filters given media depending on codecs to include.
    /// </summary>
    /// <param name="isAdvancedMode">Whether to check codec or just subtitle type.</param>
    /// <param name="includeTextSubtitles">Whether to include text subtitles.</param>
    /// <param name="includeGraphicalSubtitles">Whether to include graphical subtitles.</param>
    /// <param name="selectedCodecs">The list of codecs to include.</param>
    /// <param name="source">the media source.</param>
    /// <returns>True if media should be handled.</returns>
    private static bool FilterMediasWithCodec(bool isAdvancedMode, bool includeTextSubtitles, bool includeGraphicalSubtitles, IReadOnlyCollection<string> selectedCodecs, MediaSourceInfo source)
    {
        var subtitleStreams = source.MediaStreams.Where(stream => stream.Type == MediaStreamType.Subtitle).ToArray();
        if (!isAdvancedMode)
        {
            return (includeTextSubtitles && subtitleStreams.All(stream => stream.IsTextSubtitleStream)) || (includeGraphicalSubtitles && subtitleStreams.Any(stream => !stream.IsTextSubtitleStream));
        }

        return subtitleStreams.All(stream => selectedCodecs.Contains(stream.Codec, StringComparer.OrdinalIgnoreCase));
    }
}
