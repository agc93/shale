using HostApp;
using HostApp.Common;
using Microsoft.Extensions.DependencyInjection;
using Shale;
using Spectre.Console;
using Spectre.Console.Cli;

var services = new ServiceCollection();
AnsiConsole.WriteLine($"Running from {Environment.CurrentDirectory}");
services.AddSingleton(new GlobalOptions { Verbose = System.Diagnostics.Debugger.IsAttached });
services.AddSingleton<IOutputWriter, AnsiConsoleWriter>();
services.AddSingleton<IGreeter, SimpleGreeter>();
var modulesPath = Path.Join(Environment.CurrentDirectory, "Modules");
services.AddPlugins<IAppPlugin>(c => c.AlwaysLoad<IGreeter>().AddSearchPath(modulesPath)); // <-- This is where Shale comes in
var app = new CommandApp(new Spectre.Console.Cli.Extensions.DependencyInjection.DependencyInjectionRegistrar(services));
app.Configure(c => {
	c.SetApplicationName("Sample Host App");
	c.AddCommand<GreetingCommand>("greet");
});
return app.Run(args);