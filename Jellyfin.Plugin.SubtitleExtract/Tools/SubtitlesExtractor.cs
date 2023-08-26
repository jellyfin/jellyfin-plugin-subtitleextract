using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.MediaInfo;
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

        private static readonly string[] _supportedFormats = { SubtitleFormat.SRT, SubtitleFormat.ASS, SubtitleFormat.SSA };

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
                var streams = video.GetMediaStreams()
                    .Where(stream => stream.IsTextSubtitleStream
                                     && stream.SupportsExternalStream
                                     && !stream.IsExternal);
                foreach (var stream in streams)
                {
                    var index = stream.Index;
                    var format = stream.Codec;

                    try
                    {
                        // SubtitleEncoder has readers only for these formats, everything else converted to SRT.
                        if (!_supportedFormats.Contains(format, StringComparison.OrdinalIgnoreCase))
                        {
                            format = SubtitleFormat.SRT;
                        }

                        _logger.LogInformation("Extracting subtitle stream {Index} from {Video} as {Format}", index, video, format);

                        await _subtitleEncoder.GetSubtitles(video, mediaSourceId, index, format, 0, 0, false, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Unable to extract subtitle File:{File}\tStreamIndex:{Index}\tCodec:{Codec}",
                            video.Path,
                            index,
                            format);
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
