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
/// Scheduled task to extract embedded attachments for immediate access in web player.
/// </summary>
public class ExtractAttachmentsTask : BaseExtractionTask
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExtractAttachmentsTask" /> class.
    /// </summary>
    /// <param name="libraryManager">Instance of <see cref="ILibraryManager"/> interface.</param>
    /// <param name="localization">Instance of <see cref="ILocalizationManager"/> interface.</param>
    /// <param name="scheduler">Instance of <see cref="ILimitedConcurrencyLibraryScheduler"/> interface.</param>
    /// <param name="extractionService">Instance of the <see cref="ExtractionService"/> class.</param>
    public ExtractAttachmentsTask(
        ILibraryManager libraryManager,
        ILocalizationManager localization,
        ILimitedConcurrencyLibraryScheduler scheduler,
        ExtractionService extractionService)
        : base(libraryManager, localization, scheduler, extractionService)
    {
    }

    /// <inheritdoc />
    public override string Key => "ExtractAttachments";

    /// <inheritdoc />
    public override string Name => "Extract Attachments";

    /// <inheritdoc />
    public override string Description => "Extracts embedded attachments.";

    /// <inheritdoc />
    protected override IReadOnlyCollection<string> GetSelectedLibraries(PluginConfiguration config) => config.SelectedAttachmentsLibraries;

    /// <inheritdoc />
    protected override Task Extract(BaseItem item, CancellationToken cancellationToken) => ExtractionService.ExtractAttachments(item, cancellationToken);
}
