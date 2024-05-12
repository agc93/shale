using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace Shale;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public static class ServiceExtensions {
	/// <summary>
	/// Load and register plugins with Shale, using all defaults.
	/// </summary>
	/// <param name="services">The DI container to use for plugin type registration.</param>
	/// <param name="sharedTypes">An optional list of types to share between the host and plugins.</param>
	/// <returns>The DI container.</returns>
	public static IServiceCollection AddPlugins(
		this IServiceCollection services,
		IEnumerable<Type>? sharedTypes = null) {
		sharedTypes ??= [];
		services.AddPlugins<IPlugin>(c => c.ShareTypes(sharedTypes.ToArray()));
		return services;
	}
	
	/// <summary>
	/// Load and register plugins with Shale, optionally configuring Shale's plugin loading/registration behaviour.
	/// </summary>
	/// <param name="services">The DI container to use for plugin type registration.</param>
	/// <param name="func">
	/// An optional configuration action to run on the Shale <see cref="PluginBuilder{TPlugin}"/>
	/// to customize Shale's behaviour.
	/// </param>
	/// <returns>The DI container.</returns>
	public static IServiceCollection AddPlugins(this IServiceCollection services,
		Func<PluginBuilder<IPlugin>, PluginBuilder<IPlugin>>? func = null) {
		var builder = new PluginBuilder<IPlugin>();
		if (func != null) {
			builder = func(builder);
		}
		var loaders = builder.BuildServices(services);
		return loaders;
	}

	/// <summary>
	/// Load and register plugins with Shale using a custom plugin type, optionally configuring Shale's plugin
	/// loading/registration behaviour.
	/// </summary>
	/// <param name="services">The DI container to use for plugin type registration.</param>
	/// <param name="func">
	/// An optional configuration action to run on the Shale <see cref="PluginBuilder{TPlugin}"/>
	/// to customize Shale's behaviour.
	/// </param>
	/// <typeparam name="TPlugin">The <see cref="IPlugin"/> implementation to use as plugin type.</typeparam>
	/// <returns>The DI container.</returns>
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