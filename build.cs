#:sdk Cake.Sdk@6.1.1
#:property IncludeAdditionalFiles=build/**/*.cs
#:property ExcludeAdditionalFiles=build/**/Except*.cs
#:package Cake.MinVer@4.0.0

var target = Argument("target", "Default");

///////////////////////////////////////////////////////////////////////////////
// SETUP
///////////////////////////////////////////////////////////////////////////////

var solutionPath = "./src/Shale.slnx";
var config = Context.MakeConfiguration(solutionPath);
Context.RegisterAll(Task, InstallTool, config);

Setup(context => config);

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .WithCriteria(c => HasArgument("rebuild") || true)
    .Does<BuildConfiguration>(static (context, config) =>
{
    foreach(var path in config.Projects.AllProjectPaths)
    {
        Information("Cleaning {0}", path);
        CleanDirectories(path + "/**/bin/" + config.Configuration);
        CleanDirectories(path + "/**/obj/" + config.Configuration);
    }
    foreach (var proj in config.Projects.AllProjects) {
        Information(proj.Type);
    }
    Information("Cleaning common files...");
    CleanDirectory(config.ArtifactsPath);
});

Task("Build")
    .IsDependentOn("Clean")
    .Does<BuildConfiguration>(static (context, config) =>
{
    Information($"Building {config.ProjectName} v{config.Version}");
    DotNetBuild(config.SolutionPath, new DotNetBuildSettings
    {
        Configuration = config.Configuration,
        NoIncremental = true,
        ArgumentCustomization = args => args.Append($"/p:Version={config.Version}").Append("/p:AssemblyVersion=1.0.0.0")
    });
});

Task("Test")
    .WithCriteria(c => config.Projects.TestProjects.Any())
    .IsDependentOn("Build")
    .Does<BuildConfiguration>(static (context, config) =>
{
    Information("Running tests...");
    DotNetTest(config.SolutionPath, new DotNetTestSettings
    {
        Configuration = config.Configuration,
        NoBuild = true
    });
});



Task("Default")
    .IsDependentOn("Test")
    .Does(() =>
{
    Information("Default task completed successfully!");
});

Task("Publish")
    .IsDependentOn("NuGet");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);