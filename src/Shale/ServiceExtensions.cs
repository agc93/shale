using Microsoft.Extensions.DependencyInjection;

namespace Shale;

public static class ServiceExtensions {
	public static IServiceCollection AddPlugins(
		this IServiceCollection services,
		IEnumerable<Type>? sharedTypes = null) {
		sharedTypes ??= [];
		services.AddPlugins<IPlugin>(c => c.ShareTypes(sharedTypes.ToArray()));
		return services;
	}

	public static IServiceCollection AddPlugins<TPlugin>(this IServiceCollection services,
		Func<PluginBuilder<TPlugin>, PluginBuilder<TPlugin>>? func = null) where TPlugin : IPlugin {
		var builder = new PluginBuilder<TPlugin>();
		if (func != null) {
			builder = func(builder);
		}
		var loaders = builder.BuildServices(services);
		return loaders;
	}
}