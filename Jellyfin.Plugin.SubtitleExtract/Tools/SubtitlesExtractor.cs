using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.MediaEncoding;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SubtitleExtract.Tools
{
    /// <summary>
    /// Helper class to extract subtitles for immediate access in web player.
    /// </summary>
    public class SubtitlesExtractor
    {
        private readonly ISubtitleEncoder _subtitleEncoder;
        private readonly ILogger<SubtitlesExtractor> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubtitlesExtractor"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
        /// <param name="subtitleEncoder">Instance of the <see cref="ISubtitleEncoder"/> interface.</param>
        public SubtitlesExtractor(
            ILogger<SubtitlesExtractor> logger,
            ISubtitleEncoder subtitleEncoder)
        {
            _subtitleEncoder = subtitleEncoder;
            _logger = logger;
        }

        /// <summary>
        /// Extract subtitles from video files.
        /// </summary>
        /// <param name="video">The video to extract subtitles from.</param>
        /// <param name="cancellationToken">Token to cancel async process.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task Run(BaseItem video, CancellationToken cancellationToken)
        {
            try
            {
                var mediaSourceId = video.Id.ToString("N", CultureInfo.InvariantCulture);
                var subtitleMediaStream = video
                    .GetMediaStreams()
                    .FirstOrDefault(stream => stream is { IsTextSubtitleStream: true, SupportsExternalStream: true, IsExternal: false });
                if (subtitleMediaStream is not null)
                {
                    try
                    {
                        _logger.LogDebug("Extracting subtitles from {Video}", video.Path);
                        await _subtitleEncoder.GetSubtitles(video, mediaSourceId, subtitleMediaStream.Index, subtitleMediaStream.Codec, 0, 0, false, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Unable to extract subtitle File:{File}",
                            video.Path);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to get streams for File:{File}", video.Path);
            }
        }
    }
}
