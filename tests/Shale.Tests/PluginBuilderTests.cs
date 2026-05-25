using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Shale.Tests;

public class PluginBuilderTests {
    // The MSBuild copy target puts the test plugin here relative to the test output directory.
    private static readonly string PluginSearchPath =
        Path.Combine(AppContext.BaseDirectory, "testplugins");

    [Fact]
    public void BuildServices_DiscoversPlugin_AndInvokesConfigureServices() {
        var services = new ServiceCollection();

        new PluginBuilder<IPlugin>()
            .AddSearchPath(PluginSearchPath)
            .BuildServices(services);

        Assert.Contains(services, d => d.ServiceType.FullName == "Shale.TestPlugin.TestPluginMarker");
    }

    [Fact]
    public void BuildServices_OnlyLoadIf_False_SuppressesConfigureServices() {
        var services = new ServiceCollection();

        new PluginBuilder<IPlugin>()
            .AddSearchPath(PluginSearchPath)
            .OnlyLoadIf(_ => false)
            .BuildServices(services);

        Assert.DoesNotContain(services, d => d.ServiceType.FullName == "Shale.TestPlugin.TestPluginMarker");
    }

    [Fact]
    public void BuildServices_RunOnLoad_IsInvoked() {
        var services = new ServiceCollection();
        var invoked = false;

        new PluginBuilder<IPlugin>()
            .AddSearchPath(PluginSearchPath)
            .RunOnLoad(_ => invoked = true)
            .BuildServices(services);

        Assert.True(invoked);
    }

    [Fact]
    public void BuildServices_CustomSearchConvention_IsInvoked() {
        var services = new ServiceCollection();
        var invoked = false;

        new PluginBuilder<IPlugin>()
            .AddSearchConvention(searchPath => {
                invoked = true;
                return [];
            })
            .BuildServices(services);

        Assert.True(invoked);
    }

    [Fact]
    public void BuildServices_RegistersLoadersForDisposal() {
        var services = new ServiceCollection();

        new PluginBuilder<IPlugin>()
            .AddSearchPath(PluginSearchPath)
            .BuildServices(services);

        Assert.Contains(services, d =>
            d.Lifetime == ServiceLifetime.Singleton &&
            d.ImplementationInstance?.GetType().Name == "PluginLoader");
    }
}