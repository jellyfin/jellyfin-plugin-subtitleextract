using Jellyfin.Plugin.SubtitleExtract.Tools;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.SubtitleExtract;

/// <summary>
/// Plugin service registrator.
/// </summary>
public class SubtitleExtractPluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<SubtitleExtractor>();
    }
}
