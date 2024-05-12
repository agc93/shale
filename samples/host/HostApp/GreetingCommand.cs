using HostApp.Common;
using Spectre.Console;
using Spectre.Console.Cli;

namespace HostApp;

public class GreetingCommand(IEnumerable<IGreeter> greeters, GlobalOptions opts, IOutputWriter writer) : Command {
	public override int Execute(CommandContext context) {
		writer.Write($"Options: {opts.Verbose}");
		foreach (var greeter in greeters) {
			writer.Write(greeter.Greet(Environment.UserName));
		}
		return 0;
	}
}