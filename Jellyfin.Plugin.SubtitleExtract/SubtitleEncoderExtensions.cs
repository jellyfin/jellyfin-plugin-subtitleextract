using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.SubtitleExtract
{
    internal static class SubtitleEncoderExtensions
    {
        internal static async Task ExtractAllExtractableSubtitlesLocal(this ISubtitleEncoder encoder, MediaSourceInfo? mediaSource, CancellationToken cancellation)
        {
            if (mediaSource == null)
            {
                return;
            }

            var paths = new HashSet<Paths>();
            foreach (var stream in mediaSource.MediaStreams.Where(x => x.Type == MediaStreamType.Subtitle))
            {
                var path = await encoder.GetSubtitleFilePath(stream, mediaSource, cancellation).ConfigureAwait(false);
                if (!Path.Exists(path))
                {
                    continue;
                }

                var ext = $".{(string.IsNullOrWhiteSpace(stream.Language) ? stream.Index : stream.Language)}{Path.GetExtension(path)}";
                var dst = Path.ChangeExtension(mediaSource.Path, ext);
                if (!path.Equals(dst, StringComparison.OrdinalIgnoreCase))
                {
                    _ = paths.Add(new Paths(path, dst));
                }
            }

            foreach (var path in paths)
            {
                File.Move(path.Source, path.Dest, true);
            }
        }

        private readonly record struct Paths(string Source, string Dest);
    }
}
