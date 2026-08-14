using ApricotFramework.DiscoveryClient.AspNetCore.Extensions;
using ApricotFramework.DiscoveryClient.AspNetCore.Impl;
using ApricotFramework.DiscoveryClient.Impl;
using ApricotFramework.DiscoveryClient.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ApricotFramework.DiscoveryClient.AspNetCore.Tests;

/// <summary>
/// Covers what registration puts in the container, and that a consumer can replace each piece of it
/// from either side of the call.
/// </summary>
public class DiscoveryClientServiceCollectionExtensionsTests
{
    [Fact]
    public void AddDiscoveryClient_RegistersTheClientRegistryAndAFallbackSource()
    {
        using var provider = Build(services => services.AddDiscoveryClient(Configuration()));

        Assert.IsType<ConfigAwareStaticDiscoveryClient>(provider.GetRequiredService<IDiscoveryClient>());
        Assert.IsType<DefaultServiceRegistry>(provider.GetRequiredService<IServiceRegistry>());
        Assert.IsType<EmptyServiceDefinitionsSource>(Assert.Single(provider.GetServices<IServiceDefinitionsSource>()));
    }

    [Fact]
    public void AddDiscoveryClient_BindsTheDefaultConfigurationSection()
    {
        using var provider = Build(services => services.AddDiscoveryClient(Configuration()));

        var options = provider.GetRequiredService<IOptions<StaticDiscoveryOptions>>().Value;

        Assert.Equal(StaticHostingTypes.K8s, options.HostingType);
        Assert.Equal("istio", options.Profile);
        Assert.Equal(ServiceProtocolTypes.Https, options.DefaultProtocol);
        Assert.Equal(ServiceProtocolTypes.Https, options.DefaultGrpcProtocol);
    }

    [Fact]
    public void AddDiscoveryClient_NamedSection_BindsThatSectionInstead()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Elsewhere:HostingType"] = StaticHostingTypes.Docker })
            .Build();

        using var provider = Build(services => services.AddDiscoveryClient(configuration, "Elsewhere"));

        Assert.Equal(
            StaticHostingTypes.Docker,
            provider.GetRequiredService<IOptions<StaticDiscoveryOptions>>().Value.HostingType);
    }

    [Fact]
    public void AddDiscoveryClient_ConfiguredInCode_BindsWhatWasSet()
    {
        using var provider = Build(services => services.AddDiscoveryClient(options =>
        {
            options.HostingType = StaticHostingTypes.Localhost;
            options.DefaultProtocol = ServiceProtocolTypes.Https;
        }));

        var options = provider.GetRequiredService<IOptions<StaticDiscoveryOptions>>().Value;

        Assert.Equal(StaticHostingTypes.Localhost, options.HostingType);
        Assert.Equal(ServiceProtocolTypes.Https, options.DefaultProtocol);
    }

    [Fact]
    public void AddDiscoveryClient_NoConfigurationSectionAtAll_StillResolves()
    {
        // A host with no discovery settings has to start: every setting is optional, and the defaults
        // reach a service by its own name over HTTP.
        var empty = new ConfigurationBuilder().Build();

        using var provider = Build(services => services.AddDiscoveryClient(empty));

        Assert.NotNull(provider.GetRequiredService<IDiscoveryClient>());
        Assert.Null(provider.GetRequiredService<IOptions<StaticDiscoveryOptions>>().Value.HostingType);
    }

    [Fact]
    public void AddDiscoveryClient_CalledTwice_RegistersOneOfEachService()
    {
        using var provider = Build(services => services
            .AddDiscoveryClient(Configuration())
            .AddDiscoveryClient(Configuration()));

        Assert.Single(provider.GetServices<IDiscoveryClient>());
        Assert.Single(provider.GetServices<IServiceRegistry>());
        Assert.Single(provider.GetServices<IServiceDefinitionsSource>());
        Assert.Single(provider.GetServices<IValidateOptions<StaticDiscoveryOptions>>());
    }

    [Fact]
    public void AddDiscoveryClient_ClientRegisteredBefore_KeepsTheConsumersClient()
    {
        using var provider = Build(services => services
            .AddDiscoveryClient<StubClient>()
            .AddDiscoveryClient(Configuration()));

        Assert.IsType<StubClient>(provider.GetRequiredService<IDiscoveryClient>());
    }

    [Fact]
    public void AddDiscoveryClient_ClientRegisteredAfter_KeepsTheConsumersClient()
    {
        using var provider = Build(services => services
            .AddDiscoveryClient(Configuration())
            .AddDiscoveryClient<StubClient>());

        Assert.IsType<StubClient>(provider.GetRequiredService<IDiscoveryClient>());
    }

    [Fact]
    public void AddDiscoveryClient_ClientInstance_IsTheOneResolved()
    {
        var client = new StubClient();

        using var provider = Build(services => services
            .AddDiscoveryClient(Configuration())
            .AddDiscoveryClient(client));

        Assert.Same(client, provider.GetRequiredService<IDiscoveryClient>());
    }

    [Fact]
    public void AddServiceRegistry_RegistryRegisteredBefore_KeepsTheConsumersRegistry()
    {
        using var provider = Build(services => services
            .AddServiceRegistry<StubRegistry>()
            .AddDiscoveryClient(Configuration()));

        Assert.IsType<StubRegistry>(provider.GetRequiredService<IServiceRegistry>());
    }

    [Fact]
    public void AddServiceRegistry_Instance_IsTheOneResolved()
    {
        var registry = new StubRegistry();

        using var provider = Build(services => services
            .AddDiscoveryClient(Configuration())
            .AddServiceRegistry(registry));

        Assert.Same(registry, provider.GetRequiredService<IServiceRegistry>());
    }

    [Fact]
    public void AddServiceDefinitionsSource_AddsToTheFallbackRatherThanReplacingIt()
    {
        using var provider = Build(services => services
            .AddServiceDefinitionsSource<StubSource>()
            .AddDiscoveryClient(Configuration()));

        var sources = provider.GetServices<IServiceDefinitionsSource>().ToList();

        Assert.Equal(2, sources.Count);
        Assert.Contains(sources, source => source is StubSource);
        Assert.Contains(sources, source => source is EmptyServiceDefinitionsSource);
    }

    [Fact]
    public void AddServiceDefinitionsSource_RegisteredTwice_IsRegisteredTwice()
    {
        // Sources accumulate, so a consumer can register two instances of the same type over different
        // files. That also means a duplicated call duplicates the source.
        using var provider = Build(services => services
            .AddServiceDefinitionsSource<StubSource>()
            .AddServiceDefinitionsSource<StubSource>()
            .AddDiscoveryClient(Configuration()));

        Assert.Equal(2, provider.GetServices<IServiceDefinitionsSource>().Count(source => source is StubSource));
    }

    [Fact]
    public void AddServiceDefinitionsSource_Instance_IsResolved()
    {
        var source = new StubSource();

        using var provider = Build(services => services
            .AddServiceDefinitionsSource(source)
            .AddDiscoveryClient(Configuration()));

        Assert.Contains(source, provider.GetServices<IServiceDefinitionsSource>());
    }

    [Fact]
    public async Task AddDiscoveryClient_WithASource_ResolvesThroughTheWholeStack()
    {
        using var provider = Build(services => services
            .AddServiceDefinitionsSource<StubSource>()
            .AddDiscoveryClient(options =>
            {
                options.HostingType = StaticHostingTypes.Localhost;
                options.DefaultProtocol = ServiceProtocolTypes.Http;
            }));

        var client = provider.GetRequiredService<IDiscoveryClient>();

        Assert.Equal("http://localhost:8080", await client.GetApiBase("svc-one"));
    }

    [Fact]
    public void AddDiscoveryClient_NullArguments_Throw()
    {
        var services = new ServiceCollection();
        var configuration = Configuration();

        Assert.Throws<ArgumentNullException>(() => services.AddDiscoveryClient((IConfiguration)null!));
        Assert.Throws<ArgumentNullException>(() => services.AddDiscoveryClient(null!, "Section"));
        Assert.Throws<ArgumentNullException>(() => services.AddDiscoveryClient((Action<StaticDiscoveryOptions>)null!));
        Assert.Throws<ArgumentNullException>(() => services.AddDiscoveryClient((IDiscoveryClient)null!));
        Assert.Throws<ArgumentNullException>(() => services.AddServiceDefinitionsSource(null!));
        Assert.Throws<ArgumentNullException>(() => services.AddServiceRegistry((IServiceRegistry)null!));

        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddDiscoveryClient(configuration));
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddDiscoveryClient<StubClient>());
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddServiceRegistry<StubRegistry>());
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddServiceDefinitionsSource<StubSource>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddDiscoveryClient_BlankSectionName_Throws(string sectionName)
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentException>(() => services.AddDiscoveryClient(Configuration(), sectionName));
    }

    [Fact]
    public void AddDiscoveryClient_NullSectionName_Throws()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() => services.AddDiscoveryClient(Configuration(), null!));
    }

    [Fact]
    public void ConfigurationSectionName_IsTheKeyConsumersWriteInAppsettings()
    {
        Assert.Equal("StaticDiscovery", DiscoveryClientServiceCollectionExtensions.ConfigurationSectionName);
    }

    /// <summary>
    /// Builds a provider with scope validation on, so a lifetime mistake fails the test.
    /// </summary>
    /// <param name="register">Registers the services under test.</param>
    /// <returns>The provider.</returns>
    private static ServiceProvider Build(Action<IServiceCollection> register)
    {
        var services = new ServiceCollection();

        register(services);

        return services.BuildServiceProvider(validateScopes: true);
    }

    /// <summary>
    /// The configuration the default-section tests bind from.
    /// </summary>
    /// <returns>The configuration.</returns>
    private static IConfiguration Configuration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["StaticDiscovery:HostingType"] = StaticHostingTypes.K8s,
                ["StaticDiscovery:Profile"] = "istio",
                ["StaticDiscovery:DefaultProtocol"] = ServiceProtocolTypes.Https,
                ["StaticDiscovery:DefaultGrpcProtocol"] = ServiceProtocolTypes.Https,
            })
            .Build();
    }

    /// <summary>
    /// A source declaring one service on one port.
    /// </summary>
    private sealed class StubSource : IServiceDefinitionsSource
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
                        Ports =
                        [
                            new ServicePortDefinition
                            {
                                Protocol = ServiceProtocolTypes.Http,
                                Roles = [ServicePortRoles.Api],
                                Port = 8080,
                            },
                        ],
                    },
                },
            };
        }
    }

    /// <summary>
    /// A client that resolves nothing, standing in for a consumer's own implementation.
    /// </summary>
    private sealed class StubClient : IDiscoveryClient
    {
        /// <inheritdoc />
        public ValueTask<string?> GetApiBase(string service) => default;

        /// <inheritdoc />
        public ValueTask<string?> GetApiUrl(string service, string api) => default;

        /// <inheritdoc />
        public ValueTask<string?> GetGrpcUrl(string service) => default;

        /// <inheritdoc />
        public ValueTask<string?> GetBaseUrl(string service, string role) => default;

        /// <inheritdoc />
        public ValueTask<string?> GetUrl(string service, string role, string path) => default;
    }

    /// <summary>
    /// A registry that knows nothing, standing in for a consumer's own implementation.
    /// </summary>
    private sealed class StubRegistry : IServiceRegistry
    {
        /// <inheritdoc />
        public ValueTask<IServiceDefinition?> GetDefinition(string service) => default;

        /// <inheritdoc />
        public ValueTask<IServiceDefinition?> GetDefinition(string service, string? profile) => default;
    }
}
