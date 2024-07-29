using McMaster.NETCore.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace Shale;

/// <summary>
/// Builder used when configuring plugin discovery, loading and registration. Also registers discovered types and
/// plugin service registrations with a DI container.
/// </summary>
/// <typeparam name="TPlugin">The plugin type to use.</typeparam>
public class PluginBuilder<TPlugin> where TPlugin : IPlugin {

	private Action<string>? _loggerFunc;
	private List<Type> SharedTypes { get; set; } = [typeof(TPlugin), typeof(IServiceCollection), typeof(ILogger), typeof(ILogger<>), typeof(IConfiguration)];
	private List<Type> ForceLoadTypes { get; set; } = [];
	private List<ISearchConvention> SearchConventions { get; } = [];

	/// <summary>
	/// Sets the default registration type to use when registering force-loaded types with the DI container.
	/// </summary>
	/// <remarks>
	/// This only affects the lifetime of services registered when searching for implementations of types added with
	/// the <see cref="AlwaysLoad"/> API. Services added by plugins using <see cref="IPlugin.ConfigureServices"/> are
	/// unaffected.
	/// </remarks>
	/// <exception cref="ArgumentOutOfRangeException">An invalid service lifetime has been provided.</exception>
	public RegistrationType DefaultRegistrationType {
		set =>
			_register = value switch {
				RegistrationType.None => (collection, _, _) => collection,
				RegistrationType.Singleton => (services, type, impl) => services.AddSingleton(type, impl),
				RegistrationType.Scoped => (services, type, impl) => services.AddScoped(type, impl),
				RegistrationType.Transient => (services, type, impl) => services.AddTransient(type, impl),
				_ => throw new ArgumentOutOfRangeException(nameof(DefaultRegistrationType))
			};
	}

	private bool PreferSharedTypes { get; set; }

	private Func<IServiceCollection, Type, Type, IServiceCollection> _register = (collection, type, implementationType) =>
		collection.AddSingleton(type, implementationType);

	private Action<TPlugin> LoadAction { get; set; } = _ => {};

	private Func<TPlugin, bool> GuardAction { get; set; } = _ => true;

	private List<string> SearchPaths { get; set; } = [];

	/// <summary>
	/// Creates a new <see cref="PluginBuilder{TPlugin}"/> instance. It is strongly recommended not to directly create
	/// a plugin builder, and instead rely on the extension methods on <see cref="IServiceCollection"/>.
	/// </summary>
	public PluginBuilder() {
		SearchPaths.AddRange(GetDefaultPaths());
		SearchConventions.Add(new DynamicSearchConvention(FindDefaultPluginPaths));
	}

	private static List<string> GetDefaultPaths() {
		return [
				Path.Combine(AppContext.BaseDirectory, "Plugins"),
				Path.Combine(Environment.CurrentDirectory, "Plugins"),
				Path.Combine(AppContext.BaseDirectory, "plugins"),
				Path.Combine(Environment.CurrentDirectory, "plugins")
			];
	}

	// ReSharper disable once MemberCanBePrivate.Global
	/// <summary>
	/// Adds a new search path for Shale to search when loading plugin assemblies.
	/// </summary>
	/// <param name="path">The path to search for plugins. This should generally be a path to a directory.</param>
	/// <returns>The current builder.</returns>
	public PluginBuilder<TPlugin> AddSearchPath(string path) {
		if (!string.IsNullOrWhiteSpace(path)
				&& Directory.Exists(path)
				&& (Directory.GetDirectories(path).Length != 0 || Directory.GetFiles(path).Any(f => Path.GetExtension(f) == ".dll"))) {
			var searchPath = Directory.GetFiles(path).Any(f => Path.GetExtension(f) == ".dll") ? Directory.GetParent(path)?.FullName : path;
			if (searchPath != null) SearchPaths.Add(searchPath);
		}
		return this;
	}

	/// <summary>
	/// Adds multiple new search paths for Shale to search when loading plugin assemblies.
	/// </summary>
	/// <param name="paths">
	/// The paths to search for plugins when loading plugins. Each path will generally be a path to a unique directory.
	/// </param>
	/// <returns>The current builder.</returns>
	public PluginBuilder<TPlugin> AddSearchPaths(IEnumerable<string> paths) {
		var pathsList = paths.ToList();
		if (pathsList.Count > 0) {
			foreach (var path in pathsList) {
				AddSearchPath(path);
			}
		}
		return this;
	}

	// ReSharper disable once MemberCanBePrivate.Global
	/// <summary>
	/// Adds a new relative search path for Shale to search when loading plugin assemblies. Shale will search for this
	/// path relative to both the current directory and the executable directory.
	/// </summary>
	/// <param name="relativePath">
	/// The relative path to search for plugins. This is often just a folder name like "Plugins".
	/// </param>
	/// <returns>The current builder.</returns>
	public PluginBuilder<TPlugin> AddRelativeSearchPath(string relativePath) {
		if (!string.IsNullOrWhiteSpace(relativePath)) {
			var candidates = new List<string> {
				Path.Combine(AppContext.BaseDirectory, relativePath),
				Path.Combine(Environment.CurrentDirectory, relativePath)
			};
			foreach (var candidatePath in candidates) {
				AddSearchPath(candidatePath);
			}
		}
		return this;
	}

	/// <summary>
	/// Adds multiple new search paths for Shale to search when loading plugin assemblies. Shale will search for these
	/// paths relative to both the current directory and the executable directory.
	/// </summary>
	/// <param name="relativePaths">
	/// The relative paths to search for plugins when loading plugins. 
	/// This is often just a folder name like "Plugins".
	/// </param>
	/// <returns>The current builder.</returns>
	public PluginBuilder<TPlugin> AddRelativeSearchPaths(IEnumerable<string> relativePaths) {
		var pathsList = relativePaths.ToList();
		if (pathsList.Count > 0) {
			foreach (var path in pathsList) {
				AddRelativeSearchPath(path);
			}
		}
		return this;
	}

	/// <summary>
	/// Use an <see cref="ILogger"/> to log debug messages from Shale.
	/// </summary>
	/// <param name="logger">The logger to use.</param>
	/// <returns>The current builder.</returns>
	/// <remarks>Messages from Shale will be logged as Debug messages.</remarks>
	public PluginBuilder<TPlugin> UseLogger(ILogger logger) {
		_loggerFunc = msg => logger.LogDebug("{Message}", msg);
		return this;
	}

	/// <summary>
	/// Adds a factory for an <see cref="ILogger"/> to use when logging debug messages from Shale. This factory will be
	/// invoked each time a debug message is logged.
	/// </summary>
	/// <param name="loggerFunc">A function returning an <see cref="ILogger"/> implementation.</param>
	/// <returns>The current builder.</returns>
	/// <remarks>Messages from Shale will be logged as Debug messages.</remarks>
	public PluginBuilder<TPlugin> UseLogger(Func<ILogger> loggerFunc) {
		_loggerFunc = msg => loggerFunc().LogDebug("{Message}", msg);
		return this;
	}

	/// <summary>
	/// Logs Shale debugging messages to the console.
	/// </summary>
	/// <returns>The current builder.</returns>
	/// <remarks>Messages will be written with a `PluginLoader: ` prefix.</remarks>
	public PluginBuilder<TPlugin> UseConsoleLogging() {
		_loggerFunc = msg => Console.WriteLine($"PluginLoader: {msg}");
		return this;
	}

	/// <summary>
	/// Adds types to be shared between the host and plugins. See the
	/// <a href="https://github.com/natemcmaster/DotNetCorePlugins/blob/main/docs/what-are-shared-types.md">docs here</a>
	/// for more information on how this can be used.
	/// </summary>
	/// <param name="types"></param>
	/// <returns>The current builder.</returns>
	public PluginBuilder<TPlugin> ShareTypes(params Type[] types) {
		SharedTypes.AddRange(types);
		return this;
	}

	/// <summary>
	/// Adds a type to be "force-loaded" from any discovered plugin. Force loading types will always register any 
	/// types with the DI container if they are assignable to <see cref="TService"/>.
	/// </summary>
	/// <typeparam name="TService">The service to identify and register implementations for.</typeparam>
	/// <returns>The current builder.</returns>
	public PluginBuilder<TPlugin> AlwaysLoad<TService>() {
		SharedTypes.Add(typeof(TService));
		ForceLoadTypes.Add(typeof(TService));
		return this;
	}

	/// <summary>
	/// Adds types to be "force-loaded" from any discovered plugin. Force loading types will always register any types  
	/// with the DI container if they are assignable to any of the given types.
	/// </summary>
	/// <param name="types">The service types to identify and register implementations for.</param>
	/// <returns>The current builder.</returns>
	public PluginBuilder<TPlugin> AlwaysLoad(params Type[] types) {
		SharedTypes.AddRange(types);
		ForceLoadTypes.AddRange(types);
		return this;
	}

	/// <summary>
	/// Sets the default registration type to use when registering force-loaded types with the DI container.
	/// </summary>
	/// <param name="registrationType">The service lifetime for force-loaded types.</param>
	/// <remarks>
	/// This only affects the lifetime of services registered when searching for implementations of types added with
	/// the <see cref="AlwaysLoad"/> API. Services added by plugins using <see cref="IPlugin.ConfigureServices"/> are
	/// unaffected.
	/// </remarks>
	/// <returns>The current builder.</returns>
	/// <exception cref="ArgumentOutOfRangeException">An invalid service lifetime has been provided.</exception>
	public PluginBuilder<TPlugin> UseDefaultRegistration(RegistrationType registrationType) {
		DefaultRegistrationType = registrationType;
		return this;
	}

	public PluginBuilder<TPlugin> AddSearchConvention(Func<string, IEnumerable<FileInfo>> searchConvention) {
		SearchConventions.Add(new DynamicSearchConvention(searchConvention));
		return this;
	}

	public PluginBuilder<TPlugin> AddSearchConvention<TConvention>() where TConvention : ISearchConvention, new() {
		SearchConventions.Add(new TConvention());
		return this;
	}

	public PluginBuilder<TPlugin> AddSearchConvention<TConvention>(TConvention convention)
		where TConvention : ISearchConvention {
		SearchConventions.Add(convention);
		return this;
	}

	/// <summary>
	/// Adds a new action for Shale to perform when it loads a new plugin type. Whenever Shale loads a new
	/// <see cref="TPlugin"/> type, it will run this action *before* calling the plugin's <see cref="IPlugin.ConfigureServices"/>
	/// method.
	/// </summary>
	/// <param name="pluginFunc">The action to invoke on the current plugin type.</param>
	/// <returns>The current builder.</returns>
	/// <remarks>This can be useful if you want plugins to have a chance to run logic *before* service registration</remarks>
	public PluginBuilder<TPlugin> RunOnLoad(Action<TPlugin> pluginFunc) {
		LoadAction = pluginFunc;
		return this;
	}

	/// <summary>
	/// Adds an action that can be used to conditionally load plugin types. Discovered plugin types will only be loaded
	/// (i.e. have their <see cref="IPlugin.ConfigureServices"/> method invoked) if this action returns true. By
	/// default, all plugin types are loaded. This is invoked *before* any action added by <see cref="RunOnLoad"/>.
	/// </summary>
	/// <param name="pluginFunc">An action that is invoked for each plugin type. If this action returns true, the plugin
	/// will be loaded. Otherwise, only force loaded types will be registered from the plugin, if any.</param>
	/// <returns>The current builder.</returns>
	/// <remarks>
	/// This only affects whether plugin types (i.e. an <see cref="TPlugin"/> implementation) are loaded.
	/// Any types marked to always load (such as from <see cref="AlwaysLoad"/>) will be loaded regardless.
	/// </remarks>
	public PluginBuilder<TPlugin> OnlyLoadIf(Func<TPlugin, bool> pluginFunc) {
		GuardAction = pluginFunc;
		return this;
	}

	/// <summary>
	/// Changes the default behaviour during plugin loading to share all types between the host and plugins.
	/// </summary>
	/// <param name="enableTypeSharing">Whether to enable all types to be shared.</param>
	/// <remarks>
	/// See <a href="https://github.com/natemcmaster/DotNetCorePlugins/blob/main/docs/what-are-shared-types.md"> these
	/// docs </a> for more information on sharing types.
	/// </remarks>
	/// <returns>The current builder.</returns>
	public PluginBuilder<TPlugin> ShareAllTypes(bool enableTypeSharing = true) {
		PreferSharedTypes = enableTypeSharing;
		return this;
	}
	
	private IEnumerable<FileInfo> FindDefaultPluginPaths(string searchDir) {
		if (!Directory.Exists(searchDir)) yield break;
		foreach (var dir in Directory.GetDirectories(searchDir).Distinct()) {
			var dirName = Path.GetFileName(dir);
			var pluginDll = Path.Combine(dir, dirName + ".dll");
			if (File.Exists(pluginDll)) {
				_loggerFunc?.Invoke($"Plugin located! Loading {pluginDll}");
				yield return new FileInfo(pluginDll);
			}
		}
	}

	private List<PluginLoader> BuildLoaders(string pluginsDir) {
		// create plugin loaders
		// var pluginsDir = pluginSearchPath ?? Path.Combine(AppContext.BaseDirectory, "plugins");
		_loggerFunc?.Invoke($"Loading all plugins from {pluginsDir}");
		// var paths = FindPluginPaths(pluginsDir);
		var paths = SearchConventions.Aggregate(new List<FileInfo>(), (current, convention) => {
			try {
				var pluginPaths = convention.GetPluginsForSearchPath(pluginsDir).ToList();
				if (pluginPaths.Count > 0) {
					current.AddRange(pluginPaths);
				}
			} catch {
				_loggerFunc?.Invoke($"Error encountered while loading plugins from {pluginsDir}!");
			}
			return current;
		});
		// if (!Directory.Exists(pluginsDir)) return [];
		// foreach (var dir in Directory.GetDirectories(pluginsDir).Distinct()) {
		// 	var dirName = Path.GetFileName(dir);
		// 	var pluginDll = Path.Combine(dir, dirName + ".dll");
		// 	if (File.Exists(pluginDll)) {
		// 		_loggerFunc?.Invoke($"Plugin located! Loading {pluginDll}");
		// 		var loader = PluginLoader.CreateFromAssemblyFile(
		// 			pluginDll,
		// 			sharedTypes: [.. SharedTypes],
		// 			c => c.PreferSharedTypes = PreferSharedTypes
		// 		);
		// 		loaders.Add(loader);
		// 	}
		// }
		return paths.Distinct().Select(pluginDll => 
			PluginLoader.CreateFromAssemblyFile(
				pluginDll.FullName, 
				sharedTypes: [.. SharedTypes],
			c => {
				c.PreferSharedTypes = PreferSharedTypes;
			}))
			.ToList();
	}

	// ReSharper disable once MemberCanBePrivate.Global
	public IEnumerable<PluginLoader> BuildLoaders() {
		var comp = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
			? StringComparer.InvariantCultureIgnoreCase
			: StringComparer.InvariantCulture;
		var loaders = SearchPaths.Distinct(comp).Select(sp => Path.IsPathRooted(sp) 
			? sp 
			: Path.GetFullPath(sp)).SelectMany(BuildLoaders);
		return loaders;
	}

	/// <summary>
	/// Performs discovery for available plugins, loads plugin assemblies and registers any services from the plugins
	/// with the DI container.
	/// </summary>
	/// <param name="services">The DI container to register services with. A new one will be created if not provided.</param>
	/// <param name="config">An optional configuration to be provided to plugin type implementations.</param>
	/// <param name="disableLoaderInjection">
	/// Whether plugin types should be able to inject services from the host's DI container. When this is true, plugin
	/// types are added to the provided DI container and resolved from that container. When this is false, a new "clean"
	/// DI container is created on execution and plugin types are resolved from the clean container. 
	/// </param>
	/// <returns>The DI container.</returns>
	public IServiceCollection BuildServices(IServiceCollection? services = null, IConfiguration? config = null, bool disableLoaderInjection = false) {
		services ??= new ServiceCollection();
		try {
			config ??= services.BuildServiceProvider().GetService<IConfiguration>();
		} catch {
			//ignored
		}
		var provider = disableLoaderInjection ? new ServiceCollection() : services;
		var loaders = BuildLoaders();
		foreach (var loader in loaders) {
			// shake your
			var ass = loader.LoadDefaultAssembly();
			var types = ass.GetTypes();
			if (types.Any(IsPlugin)) {
				foreach (var pluginType in types.Where(IsPlugin)) {
					provider.AddSingleton(typeof(TPlugin), pluginType);
				}
			} else {
				_loggerFunc?.Invoke($"Found no compatible plugin types in {ass.FullName}");
			}

			if (ForceLoadTypes.Count > 0) {
				// current loader may not contain any force-load-enabled types
				// ReSharper disable LoopCanBeConvertedToQuery
				foreach (var flt in ForceLoadTypes) {
					foreach (var implType in types.Where(t => IsCompatible(flt, t))) {
						services = _register(services, flt, implType);
					}
				}
				// ReSharper restore LoopCanBeConvertedToQuery
			}
		}
		var allLoaders = provider.BuildServiceProvider().GetServices<TPlugin>();
		return allLoaders.Aggregate(services, (current, dynamicLoader) => {
			if (GuardAction.Invoke((dynamicLoader))) {
				LoadAction.Invoke(dynamicLoader);
				return dynamicLoader.ConfigureServices(current, config);
			} else {
				return current;
			}
		});
	}

	private static bool IsPlugin(Type t) {
		return IsCompatible(typeof(TPlugin), t);
	}

	private static bool IsCompatible(Type registration, Type implementation) {
		return registration.IsAssignableFrom(implementation) && !implementation.IsAbstract;
	}
}