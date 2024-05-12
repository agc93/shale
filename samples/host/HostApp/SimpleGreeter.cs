using HostApp.Common;

namespace HostApp;

public class SimpleGreeter : IGreeter {
	public string Greet(string name) => $"Hello {name} from the HostApp CLI!";
}