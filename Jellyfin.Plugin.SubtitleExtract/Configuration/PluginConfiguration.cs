using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.SubtitleExtract.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
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
    /// Gets or sets the list of codecs to look for when extracting subtitles (empty means all).
    /// </summary>
    public string IncludedCodecs { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of codecs to exclude from subtitles extraction (empty means none).
    /// </summary>
    public string ExcludedCodecs { get; set; } = string.Empty;
}
