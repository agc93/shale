public record BuildConfiguration(
    string SolutionPath,
    ProjectCollection Projects,
    string ProjectName = "",
    string Version = "",
    string Configuration = "Release",
    string ArtifactsPath = "./dist/"
);

public static class ContextExtensions
{
    public static BuildConfiguration MakeConfiguration(this ICakeContext ctx, 
        string solutionPath, 
        string? version = null, 
        string? configuration = null, 
        string? overrideVersion = null) {
        var helper = new VersionHelper(ctx);
        var v = helper.BuildVersion();
        var defaultProjectName = System.IO.Path.GetFileNameWithoutExtension(solutionPath);
        var solution = ctx.ParseSolution(solutionPath);
        configuration ??= ctx.Argument("configuration", "Release");
        var projects = GetProjects(solutionPath, configuration);
        var config = new BuildConfiguration(solutionPath, projects, defaultProjectName, version ?? v, configuration, "./dist/");
        return config;
    }
    
    private static ProjectCollection GetProjects(FilePath slnPath, string configuration) {
        var solution = ParseSolution(slnPath);
        var projects = solution.Projects.Where(p => p.Type != "{2150E333-8FDC-42A3-9474-1A3956D46DE8}");
        var testAssemblies = projects.Where(p => p.Name.Contains(".Tests")).Select(p => p.Path.GetDirectory() + "/bin/" + configuration + "/" + p.Name + ".dll");
        return new ProjectCollection {
            SourceProjects = projects.Where(p => !p.Name.Contains(".Tests")),
            TestProjects = projects.Where(p => p.Name.Contains(".Tests"))
        };
    
    }
}

public record ProjectCollection {
    public required IEnumerable<SolutionProject> SourceProjects {get;set;}
    public IEnumerable<DirectoryPath> SourceProjectPaths {get { return SourceProjects.Select(p => p.Path.GetDirectory()); } } 
    public required IEnumerable<SolutionProject> TestProjects {get;set;}
    public IEnumerable<DirectoryPath> TestProjectPaths { get { return TestProjects.Select(p => p.Path.GetDirectory()); } }
    public IEnumerable<SolutionProject> AllProjects { get { return SourceProjects.Concat(TestProjects); } }
    public IEnumerable<DirectoryPath> AllProjectPaths { get { return AllProjects.Select(p => p.Path.GetDirectory()); } }
}

public static partial class Program; 