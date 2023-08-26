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
    /// default = true.
    /// </summary>
    public bool ExtractionDuringLibraryScan { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the behavior for the metadata provider used on library scan/update.
    /// true - starts extraction and wait for extration to finish.
    /// false - starts extraction and continue.
    /// default = NonBlocking.
    /// </summary>
    public bool WaitExtraction { get; set; } = false;
}
