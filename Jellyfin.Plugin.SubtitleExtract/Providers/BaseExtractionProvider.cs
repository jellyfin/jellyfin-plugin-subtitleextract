using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.SubtitleExtract.Configuration;
using Jellyfin.Plugin.SubtitleExtract.Services;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;

namespace Jellyfin.Plugin.SubtitleExtract.Providers;

/// <summary>
/// Base class for the providers extracting embedded streams while library scanning.
/// </summary>
public abstract class BaseExtractionProvider : ICustomMetadataProvider<Episode>,
    ICustomMetadataProvider<Movie>,
    ICustomMetadataProvider<Video>,
    IHasItemChangeMonitor,
    IHasOrder,
    IForcedProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BaseExtractionProvider"/> class.
    /// </summary>
    /// <param name="extractionService">Instance of the <see cref="ExtractionService"/> class.</param>
    protected BaseExtractionProvider(ExtractionService extractionService)
    {
        ExtractionService = extractionService;
    }

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <summary>
    /// Gets the order in which the provider should be called. (Core provider is = 100).
    /// </summary>
    public int Order => 1000;

    /// <summary>
    /// Gets the extraction service.
    /// </summary>
    protected ExtractionService ExtractionService { get; }

    /// <inheritdoc/>
    public bool HasChanged(BaseItem item, IDirectoryService directoryService)
    {
        if (item.IsFileProtocol)
        {
            var file = directoryService.GetFile(item.Path);
            if (file is not null && item.HasChanged(file.LastWriteTimeUtc))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public Task<ItemUpdateType> FetchAsync(Episode item, MetadataRefreshOptions options, CancellationToken cancellationToken)
    {
        return Fetch(item, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ItemUpdateType> FetchAsync(Movie item, MetadataRefreshOptions options, CancellationToken cancellationToken)
    {
        return Fetch(item, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ItemUpdateType> FetchAsync(Video item, MetadataRefreshOptions options, CancellationToken cancellationToken)
    {
        return Fetch(item, cancellationToken);
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

    private async Task<ItemUpdateType> Fetch(BaseItem item, CancellationToken cancellationToken)
    {
        var config = SubtitleExtractPlugin.Current.Configuration;
        if (config.ExtractionDuringLibraryScan && ExtractionService.IsInLibraries(item, GetSelectedLibraries(config)))
        {
            await Extract(item, cancellationToken).ConfigureAwait(false);
        }

        return ItemUpdateType.None;
    }
}
