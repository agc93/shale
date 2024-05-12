namespace Shale;

internal class DynamicSearchConvention(Func<string, IEnumerable<FileInfo>> searchFunc) : ISearchConvention {
	public IEnumerable<FileInfo> GetPluginsForSearchPath(string searchPath) => searchFunc.Invoke(searchPath);
}