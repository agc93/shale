namespace Shale;

/// <summary>
/// Defines a convention for discovering plugin assemblies within a search directory.
/// </summary>
/// <remarks>
/// Shale calls <see cref="GetPluginsForSearchPath"/> once per registered search path. Multiple conventions
/// can be registered; their results are combined. The default convention looks for a <c>dirName/dirName.dll</c>
/// pattern in each subdirectory of the search path — custom conventions run alongside it, not instead of it.
/// </remarks>
public interface ISearchConvention {
	/// <summary>
	/// Returns the plugin assembly files found within <paramref name="searchPath"/>.
	/// </summary>
	/// <param name="searchPath">The directory to search. Shale guarantees this is one of the configured search paths.</param>
	/// <returns>
	/// The <see cref="FileInfo"/> instances pointing to <c>.dll</c> files that should be loaded as plugin assemblies.
	/// Return an empty enumerable if no plugins are found; do not return <see langword="null"/>.
	/// </returns>
	IEnumerable<FileInfo> GetPluginsForSearchPath(string searchPath);
}