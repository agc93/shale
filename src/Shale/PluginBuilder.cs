using McMaster.NETCore.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shale;

public class PluginBuilder<TPlugin> where TPlugin : IPlugin {

	private Action<string>? _loggerFunc;
	private List<Type> SharedTypes { get; set; } = [typeof(TPlugin), typeof(IServiceCollection), typeof(ILogger), typeof(ILogger<>), typeof(IConfiguration)];
	private List<Type> ForceLoadTypes { get; set; } = [];

	private RegistrationType _defaultRegistrationType;

	public RegistrationType DefaultRegistrationType {
		get => _defaultRegistrationType;
		set {
			_defaultRegistrationType = value;
			_register = _defaultRegistrationType switch {
				RegistrationType.None => (collection, _, _) => collection,
				RegistrationType.Singleton => (services, type, impl) => services.AddSingleton(type, impl),
				RegistrationType.Scoped => (services, type, impl) => services.AddScoped(type, impl),
				RegistrationType.Transient => (services, type, impl) => services.AddTransient(type, impl),
				_ => throw new ArgumentOutOfRangeException(nameof(DefaultRegistrationType))
			};
		}
	}

	private Func<IServiceCollection, Type, Type, IServiceCollection> _register = (collection, type, implementationType) =>
		collection.AddSingleton(type, implementationType);


	private List<string> SearchPaths { get; set; } = [];

	public PluginBuilder() {
		SearchPaths.AddRange(PluginBuilder<TPlugin>.GetDefaultPaths());
	}

	private static List<string> GetDefaultPaths() {
		return [
				Path.Combine(AppContext.BaseDirectory, "plugins"),
				Path.Combine(Environment.CurrentDirectory, "plugins")
			];
	}

	// ReSharper disable once MemberCanBePrivate.Global
	public PluginBuilder<TPlugin> AddSearchPath(string path) {
		if (!string.IsNullOrWhiteSpace(path)
				&& Directory.Exists(path)
				&& (Directory.GetDirectories(path).Length != 0 || Directory.GetFiles(path).Any(f => Path.GetExtension(f) == ".dll"))) {
			var searchPath = Directory.GetFiles(path).Any(f => Path.GetExtension(f) == ".dll") ? Directory.GetParent(path)?.FullName : path;
			if (searchPath != null) SearchPaths.Add(searchPath);
		}
		return this;
	}

	public PluginBuilder<TPlugin> AddSearchPaths(IEnumerable<string> paths) {
		var pathsList = paths.ToList();
		if (pathsList.Count > 0) {
			foreach (var path in pathsList) {
				AddSearchPath(path);
			}
		}
		return this;
	}



	public PluginBuilder<TPlugin> UseLogger(ILogger logger) {
		_loggerFunc = msg => logger.LogDebug("{Message}", msg);
		return this;
	}

	public PluginBuilder<TPlugin> UseLogger(Func<ILogger> loggerFunc) {
		_loggerFunc = msg => loggerFunc().LogDebug("{Message}", msg);
		return this;
	}

	public PluginBuilder<TPlugin> UseConsoleLogging() {
		_loggerFunc = msg => Console.WriteLine($"PluginLoader: {msg}");
		return this;
	}

	public PluginBuilder<TPlugin> ShareTypes(params Type[] types) {
		SharedTypes.AddRange(types);
		return this;
	}

	public PluginBuilder<TPlugin> AlwaysLoad<TService>() {
		ForceLoadTypes.Add(typeof(TService));
		return this;
	}

	public PluginBuilder<TPlugin> AlwaysLoad(params Type[] types) {
		SharedTypes.AddRange(types);
		ForceLoadTypes.AddRange(types);
		return this;
	}

	public PluginBuilder<TPlugin> UseDefaultRegistration(RegistrationType registrationType) {
		DefaultRegistrationType = registrationType;
		return this;
	}

	private List<PluginLoader> BuildLoaders(string pluginsDir) {
		var loaders = new List<PluginLoader>();
		// create plugin loaders
		// var pluginsDir = pluginSearchPath ?? Path.Combine(AppContext.BaseDirectory, "plugins");
		_loggerFunc?.Invoke($"Loading all plugins from {pluginsDir}");
		if (!Directory.Exists(pluginsDir)) return [];
		foreach (var dir in Directory.GetDirectories(pluginsDir).Distinct()) {
			var dirName = Path.GetFileName(dir);
			var pluginDll = Path.Combine(dir, dirName + ".dll");
			if (File.Exists(pluginDll)) {
				_loggerFunc?.Invoke($"Plugin located! Loading {pluginDll}");
				var loader = PluginLoader.CreateFromAssemblyFile(
					pluginDll,
					sharedTypes: [.. SharedTypes]
				);
				loaders.Add(loader);
			}
		}
		return loaders;
	}

	// ReSharper disable once MemberCanBePrivate.Global
	public IEnumerable<PluginLoader> BuildLoaders() {
		var loaders = SearchPaths.Distinct().Select(sp => Path.IsPathRooted(sp) 
			? sp 
			: Path.GetFullPath(sp)).SelectMany(BuildLoaders);
		return loaders;
	}

	public IServiceCollection BuildServices(IServiceCollection? services = null, IConfiguration? config = null, bool disableLoaderInjection = true) {
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
		return allLoaders.Aggregate(services, (current, dynamicLoader) => dynamicLoader.ConfigureServices(current, config));
	}

	private static bool IsPlugin(Type t) {
		return IsCompatible(typeof(TPlugin), t);
	}

	private static bool IsCompatible(Type registration, Type implementation) {
		return registration.IsAssignableFrom(implementation) && !implementation.IsAbstract;
	}
}