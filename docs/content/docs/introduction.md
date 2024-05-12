---
weight: 100
title: "Introduction"
description: "An introduction to the Shale project"
icon: "circle"
date: "2024-03-04T02:17:43+10:00"
lastmod: "2024-03-04T02:17:43+10:00"
draft: false
toc: true
---

Shale is a thin but opinionated abstraction layer over Nate McMaster's excellent [`DotNetCorePlugins`](https://github.com/natemcmaster/DotNetCorePlugins). That library is doing all the hard work of loading the assemblies and types, Shale just provides a slightly easier (and in my opinion, friendlier) reusable API for adding plugin support to your own applications.

In short, you can use Shale to quickly and easily add plugin support to your own applications with minimal code changes.

## Design

The `DotNetCorePlugins` library is an incredibly useful library that's perfect for dynamically loading code in .NET applications. I love it, and all the credit in the world to Nate for the library!

Since discovering it, I have added plugin support to many applications and tools that I work on, and I noticed that I tended to create a (very similar) API wrapper around that library for each project, so here we are and that API layer is now this project.

Basically, Shale is a very thin (and [leaky](https://en.wikipedia.org/wiki/Leaky_abstraction)) abstraction over `DotNetCorePlugins` with an opinionated and configurable API to make it easier to use that library for adding plugins to any .NET application. In particular, Shale is perfect for any application using a `Microsoft.Extensions.DependencyInjection`-compatible DI container.

## Why "Shale"

Because I'm a sucker for a) weird project names, and b) cool words. Shale is a type of clay used in the construction of bricks (aka building blocks) that's usually found in thin layers. Don't think too hard about it.
