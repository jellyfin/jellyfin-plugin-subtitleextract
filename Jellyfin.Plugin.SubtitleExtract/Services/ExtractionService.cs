using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.SubtitleExtract.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SubtitleExtract.Services;

/// <summary>
/// Shared subtitle and attachment extraction logic used by the metadata providers and the scheduled tasks.
/// </summary>
public class ExtractionService
{
    private readonly ILibraryManager _libraryManager;
    private readonly ISubtitleEncoder _subtitleEncoder;
    private readonly IAttachmentExtractor _attachmentExtractor;
    private readonly IMediaEncoder _mediaEncoder;
    private readonly IApplicationPaths _applicationPaths;
    private readonly ILogger<ExtractionService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtractionService"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="subtitleEncoder">Instance of the <see cref="ISubtitleEncoder"/> interface.</param>
    /// <param name="attachmentExtractor">Instance of the <see cref="IAttachmentExtractor"/> interface.</param>
    /// <param name="mediaEncoder">Instance of the <see cref="IMediaEncoder"/> interface.</param>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
    public ExtractionService(
        ILibraryManager libraryManager,
        ISubtitleEncoder subtitleEncoder,
        IAttachmentExtractor attachmentExtractor,
        IMediaEncoder mediaEncoder,
        IApplicationPaths applicationPaths,
        ILogger<ExtractionService> logger)
    {
        _libraryManager = libraryManager;
        _subtitleEncoder = subtitleEncoder;
        _attachmentExtractor = attachmentExtractor;
        _mediaEncoder = mediaEncoder;
        _applicationPaths = applicationPaths;
        _logger = logger;
    }

    /// <summary>
    /// Resolves the ids of the libraries selected by name.
    /// </summary>
    /// <param name="libraryNames">The selected library names.</param>
    /// <returns>The ids of the matching libraries, empty if none match.</returns>
    public Guid[] GetLibraryIds(IReadOnlyCollection<string> libraryNames)
    {
        if (libraryNames.Count == 0)
        {
            return [];
        }

        return _libraryManager.GetVirtualFolders()
            .Where(vf => libraryNames.Contains(vf.Name))
            .Select(vf => Guid.Parse(vf.ItemId))
            .ToArray();
    }

    /// <summary>
    /// Checks whether an item belongs to one of the selected libraries.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="libraryNames">The selected library names (empty means all).</param>
    /// <returns>True if the item should be handled.</returns>
    public bool IsInLibraries(BaseItem item, IReadOnlyCollection<string> libraryNames)
    {
        return libraryNames.Count == 0
            || _libraryManager.GetCollectionFolders(item).Any(folder => libraryNames.Contains(folder.Name));
    }

    /// <summary>
    /// Extracts the embedded subtitles of an item which match the configured subtitle types and codecs.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the extraction.</returns>
    public async Task ExtractSubtitles(BaseItem item, CancellationToken cancellationToken)
    {
        var config = SubtitleExtractPlugin.Current.Configuration;

        _logger.LogDebug("Extracting subtitles for: {Video}", item.Path);

        foreach (var mediaSource in item.GetMediaSources(false))
        {
            cancellationToken.ThrowIfCancellationRequested();

            // The encoder extracts all streams flagged with SupportsExternalStream in a single ffmpeg run.
            // The media sources are built fresh for each call, so unflagging the unselected streams only affects this extraction.
            var hasSelectedStream = false;
            foreach (var stream in mediaSource.MediaStreams)
            {
                if (stream.Type != MediaStreamType.Subtitle || !stream.SupportsExternalStream)
                {
                    continue;
                }

                if (IsSelectedSubtitleStream(config, stream))
                {
                    hasSelectedStream = true;
                }
                else
                {
                    stream.SupportsExternalStream = false;
                }
            }

            if (hasSelectedStream)
            {
                await _subtitleEncoder.ExtractAllExtractableSubtitles(mediaSource, cancellationToken).ConfigureAwait(false);
            }
        }

        _logger.LogDebug("Finished subtitle extraction for: {Video}", item.Path);
    }

    /// <summary>
    /// Extracts the embedded attachments (fonts) of an item which has ASS/SSA subtitles.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the extraction.</returns>
    public async Task ExtractAttachments(BaseItem item, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Extracting attachments for: {Video}", item.Path);

        foreach (var mediaSource in item.GetMediaSources(false))
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Attachments are only used to render ASS/SSA subtitles, skip everything else like the server does during playback.
            var assStreams = mediaSource.MediaStreams.Where(IsAssSubtitleStream).ToList();
            if (assStreams.Count == 0 || !mediaSource.MediaAttachments.Any(IsExtractableAttachment))
            {
                continue;
            }

            try
            {
                foreach (var inputFile in GetAttachmentInputFiles(mediaSource, assStreams))
                {
                    await _attachmentExtractor.ExtractAllAttachments(inputFile, mediaSource, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                // Don't let a single broken file abort the whole library scan or task.
                _logger.LogWarning(ex, "Unable to extract attachments for {File}", mediaSource.Path);
            }
        }

        _logger.LogDebug("Finished attachment extraction for: {Video}", item.Path);
    }

    private static bool IsSelectedSubtitleStream(PluginConfiguration config, MediaStream stream)
    {
        if (config.IsAdvancedMode)
        {
            // The "ass" selection covers SSA streams as well, the server extracts both the same way.
            var codec = string.Equals(stream.Codec, "ssa", StringComparison.OrdinalIgnoreCase) ? "ass" : stream.Codec;
            return config.SelectedCodecs.Contains(codec, StringComparer.OrdinalIgnoreCase);
        }

        return stream.IsTextSubtitleStream ? config.IncludeTextSubtitles : config.IncludeGraphicalSubtitles;
    }

    private static bool IsAssSubtitleStream(MediaStream stream)
    {
        return stream.Type == MediaStreamType.Subtitle
            && (string.Equals(stream.Codec, "ass", StringComparison.OrdinalIgnoreCase)
                || string.Equals(stream.Codec, "ssa", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsExtractableAttachment(MediaAttachment attachment)
    {
        // mjpeg attachments are cover images which are never extracted by the server.
        return !string.Equals(attachment.Codec, "mjpeg", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMksFile(string? path)
    {
        return !string.IsNullOrEmpty(path) && path.EndsWith(".mks", StringComparison.OrdinalIgnoreCase);
    }

    private IEnumerable<string> GetAttachmentInputFiles(MediaSourceInfo mediaSource, List<MediaStream> assStreams)
    {
        var mksFiles = assStreams
            .Where(stream => stream.IsExternal && IsMksFile(stream.Path))
            .Select(stream => stream.Path)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var mksFile in mksFiles)
        {
            yield return mksFile;
        }

        if (!assStreams.Exists(stream => !stream.IsExternal))
        {
            yield break;
        }

        if (mediaSource.VideoType is not (VideoType.Dvd or VideoType.BluRay))
        {
            yield return mediaSource.Path;
            yield break;
        }

        // DVD and Blu-ray folders can't be read directly, use the same concat file as the server does for playback.
        var concatPath = Path.Join(_applicationPaths.CachePath, "concat", mediaSource.Id + ".concat");
        if (!File.Exists(concatPath))
        {
            _mediaEncoder.GenerateConcatConfig(mediaSource, concatPath);
        }

        yield return concatPath;
    }
}
