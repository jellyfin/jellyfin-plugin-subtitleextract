using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.SubtitleExtract.Tools;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SubtitleExtract.Providers;

/// <summary>
/// Extracts embedded subtitles while library scanning for immediate access in web player.
/// </summary>
public class SubtitleExtractionProvider : ICustomMetadataProvider<Episode>,
    ICustomMetadataProvider<Movie>,
    ICustomMetadataProvider<Video>,
    IHasItemChangeMonitor,
    IHasOrder,
    IForcedProvider
{
    private readonly ILogger<SubtitleExtractionProvider> _logger;

    private readonly SubtitleExtractor _extractor;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubtitleExtractionProvider"/> class.
    /// </summary>
    /// <param name="subtitlesExtractor"><see cref="SubtitleExtractor"/> instance.</param>
    /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
    public SubtitleExtractionProvider(
        SubtitleExtractor subtitlesExtractor,
        ILogger<SubtitleExtractionProvider> logger)
    {
        _logger = logger;
        _extractor = subtitlesExtractor;
    }

    /// <inheritdoc />
    public string Name => SubtitleExtractPlugin.Current.Name;

    /// <summary>
    /// Gets the order in which the provider should be called. (Core provider is = 100).
    /// </summary>
    public int Order => 1000;

    /// <inheritdoc/>
    public bool HasChanged(BaseItem item, IDirectoryService directoryService)
    {
        if (item.IsFileProtocol)
        {
            var file = directoryService.GetFile(item.Path);
            if (file != null && item.DateModified != file.LastWriteTimeUtc)
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public Task<ItemUpdateType> FetchAsync(Episode item, MetadataRefreshOptions options, CancellationToken cancellationToken)
    {
        return FetchSubtitles(item, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ItemUpdateType> FetchAsync(Movie item, MetadataRefreshOptions options, CancellationToken cancellationToken)
    {
        return FetchSubtitles(item, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ItemUpdateType> FetchAsync(Video item, MetadataRefreshOptions options, CancellationToken cancellationToken)
    {
        return FetchSubtitles(item, cancellationToken);
    }

    private async Task<ItemUpdateType> FetchSubtitles(BaseItem item, CancellationToken cancellationToken)
    {
        var config = SubtitleExtractPlugin.Current!.Configuration;

        if (config.ExtractionDuringLibraryScan)
        {
            _logger.LogDebug("Extracting subtitles for: {Video}", item.Path);

            await _extractor.Run(item, cancellationToken).ConfigureAwait(false);

            _logger.LogDebug("Finished subtitle extraction for: {Video}", item.Path);
        }

        return ItemUpdateType.None;
    }
}
