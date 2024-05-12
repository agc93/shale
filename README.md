# Shale

Shale is a thin but opinionated abstraction layer over Nate McMaster's excellent [`DotNetCorePlugins`](https://github.com/natemcmaster/DotNetCorePlugins). That library is doing all the hard work of loading the assemblies and types, Shale just provides a slightly easier (and in my opinion, friendlier) reusable API for adding plugin support to your own applications.

## Building

Building Shale locally should be pretty simple. Ensure you have the .NET 8 SDK installed, and `dotnet` available in 
your `PATH`. 

First, restore required build tools:

```bash
dotnet tool restore
```

Now you can run a build with the Cake script in the repo:

```bash
dotnet cake
# or to build all artifacts
dotnet cake --target=Publish
```

This will build all the component projects and (assuming you used the `Publish` target) create all relevant build artifacts in the `dist/` folder.