---
weight: 200
title: "Quickstart"
description: "Getting started with using Shale in your projects"
icon: "rocket_launch"
date: "2024-03-04T02:17:43+10:00"
lastmod: "2024-03-04T02:17:43+10:00"
draft: false
toc: true
---

{{< alert icon="💡" context="info" text="This is a highly abridged quickstart guide! If you're not familiar with Shale and/or `DotNetCorePlugins` and/or plugins in .NET applications, we strongly recommend following the [detailed walkthrough](./walkthrough.md)" />}}

## Install the package(s)

Shale is provided as two packages to minimise the dependencies needed for your app's plugin authors: `Shale` and `Shale.Abstractions`. Like the `Microsoft.Extensions.*` packages, the `.Abstractions` package contains only the required interfaces (`IPlugin` in this case) and references to the dependent interfaces (like `IServiceCollection` and `IConfiguration`).

In general, your host application will need to reference `Shale` (to actually load the plugins) and whatever assembly your plugins will reference will need to reference `Shale.Abstractions`. 

## Load Shale during startup

Assuming your .NET app uses the `Microsoft.Extensions.DependencyInjection` APIs, you likely already have an `IServicesCollection` object in your startup, but if you don't already, you'll need to create one.

Wherever you're setting up your DI container, just add a call to Shale's `LoadPlugins` extension method:

```csharp
var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
services.AddPlugins(); // <-- This is where Shale comes in
// trimmed for brevity
```

### Configure Shale

Shale's fluent API should make it pretty easy to configure things during registration:

```csharp
var modulesPath = Path.Join(Environment.CurrentDirectory, "Modules");
services.AddPlugins<IAppPlugin>(c => {
	return c.AlwaysLoad<IGreeter>()
		.AddSearchPath(modulesPath)
		.ShareTypes(typeof(ISharedType))
		.OnlyLoadIf(p => p.Enabled == true)
		.UseConsoleLogging()
		.UseDefaultRegistration(RegistrationType.Transient);
});
```

## Load your plugins

Once you have your plugins, put the build output in any of your configured search paths, one directory per plugin, with the directory named the same as the plugin assembly file.

For example, by default Shale will search the "./Plugins" directory relative to both the current directory and the executable directory, so you could place plugins as below:


```text
│   HostApp.exe
└───Plugins
	└───Plugin1
	│	│   // trimmed for brevity
	│	│   HostApp.Common.dll
	│	│   Plugin1.dll
	│	└───Plugin1.deps.json
    └───Plugin2
		│   // trimmed for brevity
		│   HostApp.Common.dll
		│   Plugin2.dll
		└───Plugin2.deps.json
```

{{< alert icon="⚠" context="warning" text="To prevent loading unrelated assemblies, the folder must be named the same as the assembly file (the `.dll`) or Shale won't load the plugin." />}}