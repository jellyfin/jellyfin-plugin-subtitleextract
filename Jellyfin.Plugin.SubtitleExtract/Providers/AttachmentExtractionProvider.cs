using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SubtitleExtract.Providers;

/// <summary>
/// Extracts embedded attachments while library scanning for immediate access in web player.
/// </summary>
public class AttachmentExtractionProvider : ICustomMetadataProvider<Episode>,
    ICustomMetadataProvider<Movie>,
    ICustomMetadataProvider<Video>,
    IHasItemChangeMonitor,
    IHasOrder,
    IForcedProvider
{
    private readonly ILogger<AttachmentExtractionProvider> _logger;

    private readonly IAttachmentExtractor _extractor;

    /// <summary>
    /// Initializes a new instance of the <see cref="AttachmentExtractionProvider"/> class.
    /// </summary>
    /// <param name="attachmentExtractor"><see cref="IAttachmentExtractor"/> instance.</param>
    /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
    public AttachmentExtractionProvider(
        IAttachmentExtractor attachmentExtractor,
        ILogger<AttachmentExtractionProvider> logger)
    {
        _logger = logger;
        _extractor = attachmentExtractor;
    }

    /// <inheritdoc />
    public string Name => "Attachment Extractor";

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
            if (file != null && (item.DateModified != file.LastWriteTimeUtc || item.Size != file.Length))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public Task<ItemUpdateType> FetchAsync(Episode item, MetadataRefreshOptions options, CancellationToken cancellationToken)
    {
        return FetchAttachments(item, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ItemUpdateType> FetchAsync(Movie item, MetadataRefreshOptions options, CancellationToken cancellationToken)
    {
        return FetchAttachments(item, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ItemUpdateType> FetchAsync(Video item, MetadataRefreshOptions options, CancellationToken cancellationToken)
    {
        return FetchAttachments(item, cancellationToken);
    }

    private async Task<ItemUpdateType> FetchAttachments(BaseItem item, CancellationToken cancellationToken)
    {
        var config = SubtitleExtractPlugin.Current!.Configuration;

        if (config.ExtractionDuringLibraryScan)
        {
            _logger.LogDebug("Extracting attachments for: {Video}", item.Path);
            foreach (var mediaSource in item.GetMediaSources(false))
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

            _logger.LogDebug("Finished attachment extraction for: {Video}", item.Path);
        }

        return ItemUpdateType.None;
    }
}
