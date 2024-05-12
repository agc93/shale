using HostApp.Common;
using Spectre.Console;

namespace PluginClassLibrary;

/// <summary>
/// This is an example of being able to "override" a service provided by the host app.
/// The host app includes its own `IOutputWriter` implementation, but when the `GreetingPlugin`
/// registers this implementation, it is added to the host app's DI container, and any future
/// calls to the DI container for an `IOutputWriter` will return the `PrefixedConsoleWriter`
/// </summary>
public class PrefixedConsoleWriter : IOutputWriter {
	public void Write(string message, params object[] args) {
		AnsiConsole.WriteLine($"[OUTPUT] {string.Format(message, args)}");
	}
}
