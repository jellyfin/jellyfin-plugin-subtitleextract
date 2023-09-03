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
    /// Gets or sets a value indicating whether or not to extract subtitles as part of library scan.
    /// default = false.
    /// </summary>
    public bool ExtractionDuringLibraryScan { get; set; } = false;
}
