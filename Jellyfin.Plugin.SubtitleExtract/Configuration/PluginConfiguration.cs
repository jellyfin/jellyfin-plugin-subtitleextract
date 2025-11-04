#pragma warning disable CA1819 // Properties should not return arrays

using System.Linq;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.SubtitleExtract.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    private static readonly CheckboxItem[] _allSubtitleCodecs =
    [
        new("ass", "ASS (Advanced SSA) subtitle (.ass & .ssa files - often found on anime)"),
        new("DVDSUB", "DVD subtitles"),
        new("subrip", "SubRip subtitle (.srt files - most common type of subtitles)"),
        new("PGSSUB", "HDMV Presentation Graphic Stream subtitles (often found on Blu-ray)"),
        new("DVBSUB", "DVB subtitles"),
        new("eia_608", "EIA-608 closed captions"),
        new("jacosub", "JACOsub subtitle"),
        new("microdvd", "MicroDVD subtitle"),
        new("mov_text", "MOV text"),
        new("mpl2", "MPL2 subtitle"),
        new("pjs", "PJS (Phoenix Japanimation Society) subtitle"),
        new("realtext", "RealText subtitle"),
        new("sami", "SAMI subtitle"),
        new("stl", "Spruce subtitle format"),
        new("subviewer", "SubViewer subtitle"),
        new("subviewer1", "SubViewer v1 subtitle"),
        new("text", "raw UTF-8 text"),
        new("vplayer", "VPlayer subtitle"),
        new("webvtt", "WebVTT subtitle"),
        new("xsub", "XSUB"),
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
    public string[] SelectedSubtitlesLibraries { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of selected libraries to extract attachments from (empty means all).
    /// </summary>
    public string[] SelectedAttachmentsLibraries { get; set; } = [];

    /// <summary>
    /// Gets all available subtitle codecs.
    /// </summary>
    public CheckboxItem[] AllSubtitleCodecs => _allSubtitleCodecs;

    /// <summary>
    /// Gets or sets the list of codecs to include when extracting subtitle from a media.
    /// </summary>
    public string[] SelectedCodecs { get; set; } = _allSubtitleCodecs.Select(x => x.Value).Where(x => !string.IsNullOrEmpty(x)).ToArray()!;

    /// <summary>
    /// Gets or sets a value indicating whether advanced codec selection mode is enabled.
    /// </summary>
    public bool IsAdvancedMode { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether advanced codec selection mode is enabled.
    /// </summary>
    public bool IncludeTextSubtitles { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether advanced codec selection mode is enabled.
    /// </summary>
    public bool IncludeGraphicalSubtitles { get; set; } = true;
}
