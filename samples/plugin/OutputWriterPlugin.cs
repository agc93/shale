using HostApp.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PluginClassLibrary;

/// <summary>
/// This is a sample implementation of a "plugin type". As explained in the docs, this allows plugin authors 
/// to have complete control over service registrations etc with the host app's DI container.
/// In this example, we are replacing the host app's default implementation of `IOutputWriter` with our own
/// implementation that adds a prefix to the start of every line.
/// </summary>
/// <remarks>
/// Note that we don't need to register our `IGreeter` implementation here! As covered in the "Directly load types"
/// section of the walkthrough, Shale will automatically load that type into the container without us doing it
/// manually, since the host app has asked to always load `IGreeter` implementations
/// </remarks>
public class OutputWriterPlugin : IAppPlugin {
	public IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration? config) {
		services.AddSingleton<IOutputWriter, PrefixedConsoleWriter>();
		return services;
	}
}
