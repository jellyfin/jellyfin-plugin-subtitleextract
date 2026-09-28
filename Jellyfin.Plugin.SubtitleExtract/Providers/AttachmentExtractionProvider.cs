using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.SubtitleExtract.Configuration;
using Jellyfin.Plugin.SubtitleExtract.Services;
using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.SubtitleExtract.Providers;

/// <summary>
/// Extracts embedded attachments while library scanning for immediate access in web player.
/// </summary>
public class AttachmentExtractionProvider : BaseExtractionProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AttachmentExtractionProvider"/> class.
    /// </summary>
    /// <param name="extractionService">Instance of the <see cref="ExtractionService"/> class.</param>
    public AttachmentExtractionProvider(ExtractionService extractionService)
        : base(extractionService)
    {
    }

    /// <inheritdoc />
    public override string Name => "Attachment Extractor";

    /// <inheritdoc />
    protected override IReadOnlyCollection<string> GetSelectedLibraries(PluginConfiguration config) => config.SelectedAttachmentsLibraries;

    /// <inheritdoc />
    protected override Task Extract(BaseItem item, CancellationToken cancellationToken) => ExtractionService.ExtractAttachments(item, cancellationToken);
}
