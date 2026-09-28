using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.SubtitleExtract.Configuration;
using Jellyfin.Plugin.SubtitleExtract.Services;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LibraryTaskScheduler;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Tasks;

namespace Jellyfin.Plugin.SubtitleExtract.Tasks;

/// <summary>
/// Base class for the scheduled tasks extracting embedded streams of the selected libraries.
/// </summary>
public abstract class BaseExtractionTask : IScheduledTask
{
    private const int QueryPageLimit = 250;

    private static readonly BaseItemKind[] _itemTypes = [BaseItemKind.Episode, BaseItemKind.Movie];
    private static readonly MediaType[] _mediaTypes = [MediaType.Video];
    private static readonly SourceType[] _sourceTypes = [SourceType.Library];
    private static readonly DtoOptions _dtoOptions = new(false);

    private readonly ILibraryManager _libraryManager;
    private readonly ILocalizationManager _localization;
    private readonly ILimitedConcurrencyLibraryScheduler _scheduler;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseExtractionTask" /> class.
    /// </summary>
    /// <param name="libraryManager">Instance of <see cref="ILibraryManager"/> interface.</param>
    /// <param name="localization">Instance of <see cref="ILocalizationManager"/> interface.</param>
    /// <param name="scheduler">Instance of <see cref="ILimitedConcurrencyLibraryScheduler"/> interface.</param>
    /// <param name="extractionService">Instance of the <see cref="ExtractionService"/> class.</param>
    protected BaseExtractionTask(
        ILibraryManager libraryManager,
        ILocalizationManager localization,
        ILimitedConcurrencyLibraryScheduler scheduler,
        ExtractionService extractionService)
    {
        _libraryManager = libraryManager;
        _localization = localization;
        _scheduler = scheduler;
        ExtractionService = extractionService;
    }

    /// <inheritdoc />
    public abstract string Key { get; }

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public abstract string Description { get; }

    /// <inheritdoc />
    public string Category => _localization.GetLocalizedString("TasksLibraryCategory");

    /// <summary>
    /// Gets the extraction service.
    /// </summary>
    protected ExtractionService ExtractionService { get; }

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return [];
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var config = SubtitleExtractPlugin.Current.Configuration;

        // Run the extraction on each selected library, or on everything if none of the selected libraries is found
        Guid?[] parentIds = ExtractionService.GetLibraryIds(GetSelectedLibraries(config)).Select(id => (Guid?)id).ToArray();
        if (parentIds.Length == 0)
        {
            parentIds = [null];
        }

        for (var i = 0; i < parentIds.Length; i++)
        {
            var startProgress = 100d * i / parentIds.Length;
            var libraryProgress = new Progress<double>(p => progress.Report(startProgress + (p / parentIds.Length)));
            await RunExtraction(parentIds[i], libraryProgress, cancellationToken).ConfigureAwait(false);
        }

        progress.Report(100);
    }

    /// <summary>
    /// Gets the libraries selected for this kind of extraction.
    /// </summary>
    /// <param name="config">The plugin configuration.</param>
    /// <returns>The selected library names (empty means all).</returns>
    protected abstract IReadOnlyCollection<string> GetSelectedLibraries(PluginConfiguration config);

    /// <summary>
    /// Extracts the streams of the item.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the extraction.</returns>
    protected abstract Task Extract(BaseItem item, CancellationToken cancellationToken);

    private async Task RunExtraction(Guid? parentId, IProgress<double> progress, CancellationToken cancellationToken)
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

        if (parentId.HasValue)
        {
            query.ParentId = parentId.Value;
        }

        var numberOfVideos = _libraryManager.GetCount(query);

        for (var startIndex = 0; startIndex < numberOfVideos; startIndex += QueryPageLimit)
        {
            cancellationToken.ThrowIfCancellationRequested();

            query.StartIndex = startIndex;
            var videos = _libraryManager.GetItemList(query).ToArray();

            // The scheduler runs the extractions in parallel based on the server's library scan fanout concurrency
            var pageStart = 100d * startIndex / numberOfVideos;
            var pageShare = (double)videos.Length / numberOfVideos;
            var pageProgress = new Progress<double>(p => progress.Report(pageStart + (p * pageShare)));

            await _scheduler.Enqueue(
                videos,
                (video, _) => Extract(video, cancellationToken),
                pageProgress,
                cancellationToken).ConfigureAwait(false);
        }

        cancellationToken.ThrowIfCancellationRequested();
    }
}
