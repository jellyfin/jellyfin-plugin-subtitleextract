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
        var startProgress = 0d;

        var config = SubtitleExtractPlugin.Current.Configuration;
        var libs = config.SelectedAttachmentsLibraries;

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
            Limit = QueryPageLimit,
        };

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

                // Report the progress using "startProgress" that allows to track progress across multiple libraries
                progress.Report(startProgress + (100d * completedVideos / numberOfVideos / libsCount));
            }

            startIndex += QueryPageLimit;
        }

        // When done, update the startProgress to the current progress for next libraries
        startProgress += 100d * completedVideos / numberOfVideos / libsCount;
        return startProgress;
    }
}
