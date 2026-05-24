public sealed class VersionHelper(ICakeContext context)
{
    public string BuildVersion()
    {
        context.Information("Attempting MinVer...");
        var version = context.MinVer(new MinVerSettings
        {
            DefaultPreReleasePhase = "preview",
            TagPrefix = "v"
        });
        context.Information($"Building for version '{version.PackageVersion}'");
        return version.PackageVersion;
    }
}