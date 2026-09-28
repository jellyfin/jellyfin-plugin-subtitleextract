using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.SubtitleExtract.Configuration;
using Jellyfin.Plugin.SubtitleExtract.Services;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LibraryTaskScheduler;
using MediaBrowser.Model.Globalization;

namespace Jellyfin.Plugin.SubtitleExtract.Tasks;

/// <summary>
/// Scheduled task to extract embedded subtitles for immediate access in web player.
/// </summary>
public class ExtractSubtitlesTask : BaseExtractionTask
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExtractSubtitlesTask" /> class.
    /// </summary>
    /// <param name="libraryManager">Instance of <see cref="ILibraryManager"/> interface.</param>
    /// <param name="localization">Instance of <see cref="ILocalizationManager"/> interface.</param>
    /// <param name="scheduler">Instance of <see cref="ILimitedConcurrencyLibraryScheduler"/> interface.</param>
    /// <param name="extractionService">Instance of the <see cref="ExtractionService"/> class.</param>
    public ExtractSubtitlesTask(
        ILibraryManager libraryManager,
        ILocalizationManager localization,
        ILimitedConcurrencyLibraryScheduler scheduler,
        ExtractionService extractionService)
        : base(libraryManager, localization, scheduler, extractionService)
    {
    }

    /// <inheritdoc />
    public override string Key => "ExtractSubtitles";

    /// <inheritdoc />
    public override string Name => "Extract Subtitles";

    /// <inheritdoc />
    public override string Description => "Extracts embedded subtitles proactively.";

    /// <inheritdoc />
    protected override IReadOnlyCollection<string> GetSelectedLibraries(PluginConfiguration config) => config.SelectedSubtitlesLibraries;

    /// <inheritdoc />
    protected override Task Extract(BaseItem item, CancellationToken cancellationToken) => ExtractionService.ExtractSubtitles(item, cancellationToken);
}
