using System;
using System.Collections.Generic;
using Jellyfin.Plugin.SubtitleExtract.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.SubtitleExtract;

/// <summary>
/// Plugin entrypoint.
/// </summary>
public class SubtitleExtractPlugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SubtitleExtractPlugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
    public SubtitleExtractPlugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Current = this;
    }

    /// <inheritdoc />
    public override string Name => "Subtitle Extract";

    /// <inheritdoc />
    public override Guid Id => new("CD893C24-B59E-4060-87B2-184070E1BF68");

    /// <inheritdoc />
    public override string Description => "Extracts embedded subtitles";

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static SubtitleExtractPlugin Current { get; private set; } = null!;

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return [
            new PluginPageInfo
            {
                Name = "Jellyfin subtitle extractor",
                EmbeddedResourcePath = GetType().Namespace + ".Configuration.configPage.html"
            }
        ];
    }
}
