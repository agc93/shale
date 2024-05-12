using HostApp.Common;

namespace PluginClassLibrary;

/// <summary>
/// This is an example of a service that will be loaded by Shale into the host app for consumption.
/// Since it will be constructed by the DI container, we can use constructor injection to resolve 
/// services from the host app. Those services might have been registered by the host app (like 
/// `GlobalOptions` in this case), or by this plugin (like `IOutputWriter`) but the plugin doesn't
/// need to know about that!
/// </summary>
/// <remarks>
/// Since (in this example) the `IGreeter` type is configured to always load, Shale will discover
/// and register this type without us having to do anything else!
/// </remarks>
public class Greeter(GlobalOptions options, IOutputWriter writer) : IGreeter {
	public string Greet(string name) {
		if (options.Verbose) { //the plugin doesn't need to know where this value is coming from!
			//the plugin also doesn't need to know what IOutputWriter we're using, it just gets it from DI
			writer.Write("You are running in verbose mode!"); 
		}
		return $"Hello {name} from an external plugin!";
	}
}
