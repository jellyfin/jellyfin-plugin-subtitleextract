using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.SubtitleExtract.Configuration;
using Jellyfin.Plugin.SubtitleExtract.Services;
using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.SubtitleExtract.Providers;

/// <summary>
/// Extracts embedded subtitles while library scanning for immediate access in web player.
/// </summary>
public class SubtitleExtractionProvider : BaseExtractionProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SubtitleExtractionProvider"/> class.
    /// </summary>
    /// <param name="extractionService">Instance of the <see cref="ExtractionService"/> class.</param>
    public SubtitleExtractionProvider(ExtractionService extractionService)
        : base(extractionService)
    {
    }

    /// <inheritdoc />
    public override string Name => "Subtitle Extraction";

    /// <inheritdoc />
    protected override IReadOnlyCollection<string> GetSelectedLibraries(PluginConfiguration config) => config.SelectedSubtitlesLibraries;

    /// <inheritdoc />
    protected override Task Extract(BaseItem item, CancellationToken cancellationToken) => ExtractionService.ExtractSubtitles(item, cancellationToken);
}
