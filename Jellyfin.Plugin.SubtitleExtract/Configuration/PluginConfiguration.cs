using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.SubtitleExtract.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    private static readonly List<string> _allSubtitleCodecs =
    [
        "ass - ASS (Advanced SSA) subtitle (.ass & .ssa files - often found on anime)",
        "DVDSUB - DVD subtitles", "subrip - SubRip subtitle (.srt files - most common type of subtitles)",
        "PGSSUB - HDMV Presentation Graphic Stream subtitles (often found on Blu-ray)",
        "DVBSUB - DVB subtitles", "eia_608 - EIA-608 closed captions",
        "jacosub - JACOsub subtitle",
        "microdvd - MicroDVD subtitle", "mov_text - MOV text",
        "mpl2 - MPL2 subtitle",
        "pjs - PJS (Phoenix Japanimation Society) subtitle",
        "realtext - RealText subtitle",
        "sami - SAMI subtitle", "stl - Spruce subtitle format",
        "subviewer - SubViewer subtitle",
        "subviewer1 - SubViewer v1 subtitle",
        "text - raw UTF-8 text",
        "vplayer - VPlayer subtitle",
        "webvtt - WebVTT subtitle",
        "xsub - XSUB"
    ];

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
    }

    /// <summary>
    /// Gets or sets a value indicating whether or not to extract subtitles and attachments as part of library scan.
    /// default = false.
    /// </summary>
    public bool ExtractionDuringLibraryScan { get; set; } = false;

    /// <summary>
    /// Gets or sets the list of selected libraries to extract subtitles from (empty means all).
    /// </summary>
    public string SelectedSubtitlesLibraries { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of selected libraries to extract attachments from (empty means all).
    /// </summary>
    public string SelectedAttachmentsLibraries { get; set; } = string.Empty;

    /// <summary>
    /// Gets all available subtitle codecs.
    /// </summary>
    public string AllSubtitleCodecs => string.Join("#", _allSubtitleCodecs);

    /// <summary>
    /// Gets or sets the list of codecs to include when extracting subtitle from a media.
    /// </summary>
    public string SelectedCodecs { get; set; } = string.Join(", ", _allSubtitleCodecs);
}
