using Cake.Core;
public sealed class ReleaseTasks : ITaskModule
{
    public List<string> RequiredTools => [];
    public void Register(Func<string, CakeTaskBuilder> task, ICakeContext context, BuildConfiguration state)
    {
        task("Publish-NuGet-Package")
            .IsDependentOn("NuGet")
            .WithCriteria(() => context.HasEnvironmentVariable("NUGET_TOKEN"))
            .WithCriteria(() => context.HasEnvironmentVariable("GITHUB_REF"))
            .WithCriteria(() => context.EnvironmentVariable("GITHUB_REF").StartsWith("refs/tags/v") || context.EnvironmentVariable("GITHUB_REF") == "refs/heads/main")
            .Does<BuildConfiguration>(PublishNuGetPackage);
        
        task("NuGet")
            .IsDependentOn("Build")
            .IsDependeeOf("Default")
            .Does<BuildConfiguration>(BuildNuGetPackage);
        
        
        task("Release")
            .IsDependentOn("Publish")
            .IsDependentOn("Publish-NuGet-Package");
    }

    public static void PublishNuGetPackage(ICakeContext context, BuildConfiguration config) {
        var nugetToken = context.EnvironmentVariable("NUGET_TOKEN");
        var pkgFiles = context.GetFiles($"{config.ArtifactsPath}package/*.nupkg");
        context.Information($"Pushing {pkgFiles.Count()} package files!");
        foreach (var pkg in pkgFiles) {
            context.DotNetNuGetPush(pkg.ToString(), new DotNetNuGetPushSettings {
                Source = "https://api.nuget.org/v3/index.json",
                ApiKey = nugetToken
            });
        }
    }

    public static void BuildNuGetPackage(ICakeContext context, BuildConfiguration config) {
        context.Information("Building NuGet package");
        context.CreateDirectory(config.ArtifactsPath + "package/");
        var packSettings = new DotNetPackSettings {
            Configuration = config.Configuration,
            NoBuild = false,
            OutputDirectory = $"{config.ArtifactsPath}package",
            ArgumentCustomization = args => args
                .Append($"/p:Version=\"{config.Version}\"")
                .Append("/p:NoWarn=\"NU1701 NU1602\"")
        };
        context.DotNetPack(config.SolutionPath, packSettings);
    }
}