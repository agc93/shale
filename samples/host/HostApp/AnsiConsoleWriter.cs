using HostApp.Common;
using Spectre.Console;
using System.Diagnostics.CodeAnalysis;

namespace HostApp;

[SuppressMessage("Performance", "CA1822:Mark members as static")]
public class AnsiConsoleWriter : IOutputWriter {
	public void Write(string message, params object[] args) => AnsiConsole.WriteLine(message, args);
}