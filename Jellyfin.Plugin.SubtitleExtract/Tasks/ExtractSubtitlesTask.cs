using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.SubtitleExtract.Tools;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Globalization;
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
    private readonly ILocalizationManager _localization;

    private static readonly BaseItemKind[] _itemTypes = [BaseItemKind.Episode, BaseItemKind.Movie];
    private static readonly MediaType[] _mediaTypes = [MediaType.Video];
    private static readonly SourceType[] _sourceTypes = [SourceType.Library];
    private static readonly DtoOptions _dtoOptions = new(false);

    private readonly SubtitleExtractor _extractor;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtractSubtitlesTask" /> class.
    /// </summary>
    /// <param name="libraryManager">Instance of <see cref="ILibraryManager"/> interface.</param>
    /// <param name="subtitleExtractor"><see cref="SubtitleExtractor"/> instance.</param>
    /// <param name="localization">Instance of <see cref="ILocalizationManager"/> interface.</param>
    public ExtractSubtitlesTask(
        ILibraryManager libraryManager,
        SubtitleExtractor subtitleExtractor,
        ILocalizationManager localization)
    {
        _libraryManager = libraryManager;
        _localization = localization;
        _extractor = subtitleExtractor;
    }

    /// <inheritdoc />
    public string Key => "ExtractSubtitles";

    /// <inheritdoc />
    public string Name => SubtitleExtractPlugin.Current!.Name;

    /// <inheritdoc />
    public string Description => SubtitleExtractPlugin.Current!.Description;

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

        var config = SubtitleExtractPlugin.Current!.Configuration;
        var libs = config.SelectedLibraries.Split(",").Select(v => v.Trim()).Where(v => !string.IsNullOrEmpty(v)).ToList();

        List<string> parentIds = [];
        if (libs.Count > 0)
        {
            // Try to get parent ids from the selected libraries
            parentIds = _libraryManager.GetVirtualFolders().Where(vf => libs.Contains(vf.Name)).Select(vf => vf.ItemId).ToList();
        }

        if (parentIds.Count > 0)
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
        string? parentId,
        List<string> parentIds,
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

        if (parentIds.Count > 0 && parentId != null)
        {
            // In case parent is provided, add its Guid to the query
            query.ParentId = Guid.ParseExact(parentId, "N");
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

                await _extractor.Run(video, cancellationToken).ConfigureAwait(false);

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
