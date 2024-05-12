namespace HostApp.Common;

public interface IGreeter {
	string Greet(string name);
}

public class GlobalOptions {
	public bool Verbose { get; set; }
}

public interface IOutputWriter {
	void Write(string message, params object[] args);
}