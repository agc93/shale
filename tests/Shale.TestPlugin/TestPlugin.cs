using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shale;

namespace Shale.TestPlugin;

public class TestPlugin : IPlugin {
    public IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration? config) {
        services.AddSingleton<TestPluginMarker>();
        return services;
    }
}