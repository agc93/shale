namespace Shale;

public interface ISearchConvention {
	IEnumerable<FileInfo> GetPluginsForSearchPath(string searchPath);
}