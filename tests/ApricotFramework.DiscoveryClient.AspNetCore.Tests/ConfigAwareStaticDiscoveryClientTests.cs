using ApricotFramework.DiscoveryClient.AspNetCore.Extensions;
using ApricotFramework.DiscoveryClient.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ApricotFramework.DiscoveryClient.AspNetCore.Tests;

/// <summary>
/// Covers the two things the configuration-aware client exists for: following a reload, and failing
/// at startup on settings that could never work.
/// </summary>
public class ConfigAwareStaticDiscoveryClientTests
{
    [Fact]
    public async Task GetApiBase_ConfigurationReloaded_ResolvesWithTheNewSettings()
    {
        // The point of reading the options on every call: switching profile is a configuration change,
        // not a rollout.
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["StaticDiscovery:HostingType"] = StaticHostingTypes.Localhost,
                ["StaticDiscovery:DefaultProtocol"] = ServiceProtocolTypes.Https,
            })
            .Build();

        var services = new ServiceCollection();
        services.AddServiceDefinitionsSource(new ProfiledSource());
        services.AddDiscoveryClient(configuration);

        using var provider = services.BuildServiceProvider(validateScopes: true);

        var client = provider.GetRequiredService<IDiscoveryClient>();

        Assert.Equal("https://localhost:8443", await client.GetApiBase("svc-one"));

        configuration["StaticDiscovery:Profile"] = "istio";
        configuration.Reload();

        Assert.Equal("https://localhost:443", await client.GetApiBase("svc-one"));
    }

    [Fact]
    public async Task GetApiBase_ProtocolChangedByReload_ResolvesTheOtherPort()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["StaticDiscovery:HostingType"] = StaticHostingTypes.Localhost,
                ["StaticDiscovery:DefaultProtocol"] = ServiceProtocolTypes.Http,
            })
            .Build();

        var services = new ServiceCollection();
        services.AddServiceDefinitionsSource(new ProfiledSource());
        services.AddDiscoveryClient(configuration);

        using var provider = services.BuildServiceProvider(validateScopes: true);

        var client = provider.GetRequiredService<IDiscoveryClient>();

        Assert.Equal("http://localhost:8080", await client.GetApiBase("svc-one"));

        configuration["StaticDiscovery:DefaultProtocol"] = ServiceProtocolTypes.Https;
        configuration.Reload();

        Assert.Equal("https://localhost:8443", await client.GetApiBase("svc-one"));
    }

    [Fact]
    public void Options_UnusableSettings_ThrowOnFirstAccess()
    {
        // In a bare provider there is no startup hook to run the validator, so the failure lands on
        // first access to the options instead. Either way it is raised, not silently resolved to null.
        var services = new ServiceCollection();
        services.AddDiscoveryClient(options => options.HostingType = "kubernetes");

        using var provider = services.BuildServiceProvider(validateScopes: true);

        var exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<StaticDiscoveryOptions>>().Value);

        Assert.Contains("kubernetes", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Host_UnusableSettings_FailsToStart()
    {
        // What an operator actually sees: the host does not come up, with the mistake in the message.
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddDiscoveryClient(options => options.DefaultProtocol = "tcp");

        using var host = builder.Build();

        var exception = await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync(TestContext.Current.CancellationToken));

        Assert.Contains("tcp", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Host_UsableSettings_Starts()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddDiscoveryClient(options => options.HostingType = StaticHostingTypes.K8s);

        using var host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// A source declaring one service with an <c>istio</c> profile that moves its api port to 443.
    /// </summary>
    private sealed class ProfiledSource : IServiceDefinitionsSource
    {
        /// <inheritdoc />
        public ServiceDefinitions? GetDefinitions()
        {
            return new ServiceDefinitions
            {
                Services = new Dictionary<string, ServiceDefinition>(StringComparer.Ordinal)
                {
                    ["svc-one"] = new ServiceDefinition
                    {
                        Ports = [Port(ServiceProtocolTypes.Http, 8080), Port(ServiceProtocolTypes.Https, 8443)],
                        Overrides = new Dictionary<string, ServiceDefinition>(StringComparer.Ordinal)
                        {
                            ["istio"] = new ServiceDefinition { Ports = [Port(ServiceProtocolTypes.Https, 443)] },
                        },
                    },
                },
            };
        }

        /// <summary>
        /// Builds an api port on a protocol.
        /// </summary>
        /// <param name="protocol">The protocol.</param>
        /// <param name="port">The port number.</param>
        /// <returns>The port definition.</returns>
        private static ServicePortDefinition Port(string protocol, int port)
        {
            return new ServicePortDefinition
            {
                Protocol = protocol,
                Roles = [ServicePortRoles.Api],
                Port = port,
            };
        }
    }
}
