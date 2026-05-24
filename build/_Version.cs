public sealed class VersionHelper(ICakeContext context)
{
    public string FallbackVersion =>
        context.Argument<string>(
            "force-version",
            context.EnvironmentVariable("FALLBACK_VERSION") ?? "0.0.1");

    public string BuildVersion(string? fallbackVersion = null)
    {
        fallbackVersion ??= FallbackVersion;
        var packageVersion = string.Empty;
        try
        {
            context.Information("Attempting MinVer...");
            var settings = new MinVerSettings
            {
                DefaultPreReleasePhase = "preview",
                TagPrefix = "v"
            };
            var version = context.MinVer(settings);
            packageVersion = version.PackageVersion;
        }
        catch (Exception ex)
        {
            context.Warning($"Error when getting version {ex.Message}");
            context.Information($"Falling back to version: {fallbackVersion}");
            packageVersion = fallbackVersion;
        }
        finally
        {
            context.Information($"Building for version '{packageVersion}'");
        }

        return packageVersion;
    }
}