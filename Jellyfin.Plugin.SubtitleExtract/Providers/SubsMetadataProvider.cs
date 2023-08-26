using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.SubtitleExtract.Tools;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SubtitleExtract.Providers
{
    internal class SubsMetadataProvider : ICustomMetadataProvider<Episode>,
        ICustomMetadataProvider<Movie>,
        ICustomMetadataProvider<Video>,
        IHasItemChangeMonitor,
        IHasOrder,
        IForcedProvider
    {
        private readonly ISubtitleEncoder _subtitleEncoder;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<SubsMetadataProvider> _logger;

        public SubsMetadataProvider(
            ISubtitleEncoder subtitleEncoder,
            ILoggerFactory loggerFactory,
            ILogger<SubsMetadataProvider> logger)
        {
            _subtitleEncoder = subtitleEncoder;
            _logger = logger;
            _loggerFactory = loggerFactory;
        }

        /// <inheritdoc />
        public string Name => "Jellyfin Subtitle Extractor";

        /// <summary>
        /// Gets the order in which the provider should be called. (Core provider is = 100).
        /// </summary>
        public int Order => 1000;

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

        public Task<ItemUpdateType> FetchAsync(Episode item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            return FetchSubtitles(item, cancellationToken);
        }

        public Task<ItemUpdateType> FetchAsync(Movie item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            return FetchSubtitles(item, cancellationToken);
        }

        public Task<ItemUpdateType> FetchAsync(Video item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            return FetchSubtitles(item, cancellationToken);
        }

        private async Task<ItemUpdateType> FetchSubtitles(BaseItem item, CancellationToken cancellationToken)
        {
            var config = SubtitleExtractPlugin.Current!.Configuration;

            if (config.ExtractionDuringLibraryScan)
            {
                var extractor = new SubtitlesExtractor(_loggerFactory.CreateLogger<SubtitlesExtractor>(), _subtitleEncoder);
                _logger.LogInformation("Extracting subtitles for: {Video}", item);

                if (config.WaitExtraction)
                {
                    _ = extractor.Run(item, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await extractor.Run(item, cancellationToken).ConfigureAwait(false);
                }

                _logger.LogInformation("Finished subtitle extraction for: {Video}", item);
            }

            return ItemUpdateType.None;
        }
    }
}