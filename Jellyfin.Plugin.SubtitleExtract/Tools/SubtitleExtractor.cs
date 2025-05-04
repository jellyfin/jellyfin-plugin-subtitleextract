using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SubtitleExtract.Tools
{
    /// <summary>
    /// Helper class to extract subtitles for immediate access in web player.
    /// </summary>
    public class SubtitleExtractor
    {
        private readonly ISubtitleEncoder _subtitleEncoder;
        private readonly ILogger<SubtitleExtractor> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubtitleExtractor"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
        /// <param name="subtitleEncoder">Instance of the <see cref="ISubtitleEncoder"/> interface.</param>
        public SubtitleExtractor(
            ILogger<SubtitleExtractor> logger,
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
                var config = SubtitleExtractPlugin.Current!.Configuration;
                // Retrieve le list of codecs to check. In case none is provided, do not check codecs at all.
                var codecs = config.IncludedCodecs.Trim().Split(",").Select(v => v.Trim()).Where(v => !string.IsNullOrEmpty(v)).ToList();

                var mediaSourceId = video.Id.ToString("N", CultureInfo.InvariantCulture);
                var extractableSubtitleMediaStream = video
                    .GetMediaStreams()
                    .FirstOrDefault(s => s is { IsExtractableSubtitleStream: true, SupportsExternalStream: true } && (codecs.Count == 0 || codecs.Contains(s.Codec, StringComparer.CurrentCultureIgnoreCase)));
                if (extractableSubtitleMediaStream is null)
                {
                    return;
                }

                try
                {
                    _logger.LogDebug("Extracting subtitles from {Video}", video.Path);
                    // Just asking for a subtitle will force the encoder to extract all subtitles.
                    var subtitleStream = await _subtitleEncoder.GetSubtitles(video, mediaSourceId, extractableSubtitleMediaStream.Index, SubtitleFormat.SRT, 0, 0, false, cancellationToken).ConfigureAwait(false);
                    await using (subtitleStream.ConfigureAwait(false))
                    {
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Unable to extract subtitle File:{File}",
                        video.Path);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to get streams for File:{File}", video.Path);
            }
        }
    }
}
