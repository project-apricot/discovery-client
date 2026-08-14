using ApricotFramework.DiscoveryClient.Impl;
using ApricotFramework.DiscoveryClient.Model;

namespace ApricotFramework.DiscoveryClient.Tests;

/// <summary>
/// Covers how the registry merges sources and applies a profile.
/// </summary>
public class DefaultServiceRegistryTests
{
    [Fact]
    public async Task GetDefinition_UnknownService_ReturnsNull()
    {
        var registry = Catalogues.RegistryOver(Catalogues.Reference());

        Assert.Null(await registry.GetDefinition("no-such-service"));
        Assert.Null(await registry.GetDefinition("no-such-service", "istio"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetDefinition_BlankService_ReturnsNull(string service)
    {
        var registry = Catalogues.RegistryOver(Catalogues.Reference());

        Assert.Null(await registry.GetDefinition(service));
    }

    [Fact]
    public async Task GetDefinition_NoProfile_ReturnsTheDefinitionAsWritten()
    {
        var registry = Catalogues.RegistryOver(Catalogues.Reference());

        var definition = await registry.GetDefinition("svc-one", profile: null);

        Assert.NotNull(definition);
        Assert.Equal("namespace-one", definition.Namespace);
        Assert.Equal(6, definition.Ports!.Count());
    }

    [Fact]
    public async Task GetDefinition_Profile_AppendsItsPortsAfterTheServicesOwn()
    {
        // Appending rather than replacing is what makes the override win, because a lookup takes the
        // last port matching its protocol and role. It also leaves roles the override omits reachable.
        var registry = Catalogues.RegistryOver(Catalogues.Reference());

        var definition = await registry.GetDefinition("svc-one", "istio");

        Assert.NotNull(definition);

        var ports = definition.Ports!.ToList();

        Assert.Equal(8, ports.Count);
        Assert.Equal(443, ports[6].Port);
        Assert.Equal(443, ports[7].Port);
    }

    [Fact]
    public async Task GetDefinition_ProfileNamingNamespaceAndDomain_ReplacesThem()
    {
        var catalogue = Catalogues.One("svc-one", new ServiceDefinition
        {
            Namespace = "base-namespace",
            Domain = "base.cluster",
            Overrides = new Dictionary<string, ServiceDefinition>(StringComparer.Ordinal)
            {
                ["other"] = new ServiceDefinition { Namespace = "other-namespace", Domain = "other.cluster" },
            },
        });

        var definition = await Catalogues.RegistryOver(catalogue).GetDefinition("svc-one", "other");

        Assert.NotNull(definition);
        Assert.Equal("other-namespace", definition.Namespace);
        Assert.Equal("other.cluster", definition.Domain);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetDefinition_ProfileLeavingAValueBlank_KeepsTheOriginal(string? blank)
    {
        var catalogue = Catalogues.One("svc-one", new ServiceDefinition
        {
            Namespace = "base-namespace",
            Domain = "base.cluster",
            Overrides = new Dictionary<string, ServiceDefinition>(StringComparer.Ordinal)
            {
                ["other"] = new ServiceDefinition { Namespace = blank, Domain = blank },
            },
        });

        var definition = await Catalogues.RegistryOver(catalogue).GetDefinition("svc-one", "other");

        Assert.NotNull(definition);
        Assert.Equal("base-namespace", definition.Namespace);
        Assert.Equal("base.cluster", definition.Domain);
    }

    [Fact]
    public async Task GetDefinition_ProfileOnAServiceWithNoPorts_ReturnsOnlyTheProfilesPorts()
    {
        var catalogue = Catalogues.One("svc-one", new ServiceDefinition
        {
            Ports = null,
            Overrides = new Dictionary<string, ServiceDefinition>(StringComparer.Ordinal)
            {
                ["other"] = new ServiceDefinition { Ports = [Catalogues.Port("http", ServicePortRoles.Api, 443)] },
            },
        });

        var definition = await Catalogues.RegistryOver(catalogue).GetDefinition("svc-one", "other");

        Assert.NotNull(definition);
        Assert.Single(definition.Ports!);
    }

    [Fact]
    public async Task GetDefinition_ProfileWithNoPorts_KeepsTheServicesOwn()
    {
        var catalogue = Catalogues.One("svc-one", new ServiceDefinition
        {
            Ports = [Catalogues.Port("http", ServicePortRoles.Api, 8080)],
            Overrides = new Dictionary<string, ServiceDefinition>(StringComparer.Ordinal)
            {
                ["other"] = new ServiceDefinition { Namespace = "elsewhere" },
            },
        });

        var definition = await Catalogues.RegistryOver(catalogue).GetDefinition("svc-one", "other");

        Assert.NotNull(definition);
        Assert.Single(definition.Ports!);
        Assert.Equal("elsewhere", definition.Namespace);
    }

    [Fact]
    public async Task GetDefinition_OneArgumentOverload_IgnoresProfiles()
    {
        var registry = Catalogues.RegistryOver(Catalogues.Reference());

        var definition = await registry.GetDefinition("svc-one");

        Assert.NotNull(definition);
        Assert.Equal(6, definition.Ports!.Count());
    }

    [Fact]
    public async Task Constructor_TwoSourcesDeclaringOneService_TheLaterOneWinsOutright()
    {
        var first = Catalogues.One("svc-one", new ServiceDefinition
        {
            Namespace = "first",
            Ports = [Catalogues.Port("http", ServicePortRoles.Api, 1111)],
        });

        var second = Catalogues.One("svc-one", new ServiceDefinition
        {
            Namespace = "second",
            Ports = [Catalogues.Port("http", ServicePortRoles.Api, 2222)],
        });

        var definition = await Catalogues.RegistryOver(first, second).GetDefinition("svc-one");

        Assert.NotNull(definition);
        Assert.Equal("second", definition.Namespace);
        Assert.Single(definition.Ports!);
        Assert.Equal(2222, definition.Ports!.Single().Port);
    }

    [Fact]
    public async Task Constructor_SourcesDeclaringDifferentServices_MergesThem()
    {
        var first = Catalogues.One("svc-one", new ServiceDefinition());
        var second = Catalogues.One("svc-two", new ServiceDefinition());

        var registry = Catalogues.RegistryOver(first, second);

        Assert.NotNull(await registry.GetDefinition("svc-one"));
        Assert.NotNull(await registry.GetDefinition("svc-two"));
    }

    [Fact]
    public async Task Constructor_SourceReturningNull_IsIgnored()
    {
        var registry = Catalogues.RegistryOver(null, Catalogues.Reference(), null);

        Assert.NotNull(await registry.GetDefinition("svc-one"));
    }

    [Fact]
    public async Task Constructor_SourceReturningNoServices_IsIgnored()
    {
        var registry = Catalogues.RegistryOver(new ServiceDefinitions { Services = null }, Catalogues.Reference());

        Assert.NotNull(await registry.GetDefinition("svc-one"));
    }

    [Fact]
    public async Task Constructor_ServiceWithANullDefinition_IsDropped()
    {
        // What a catalogue naming a service and saying nothing about it parses to.
        var catalogue = new ServiceDefinitions
        {
            Services = new Dictionary<string, ServiceDefinition>(StringComparer.Ordinal) { ["svc-one"] = null! },
        };

        Assert.Null(await Catalogues.RegistryOver(catalogue).GetDefinition("svc-one"));
    }

    [Fact]
    public async Task Constructor_LaterSourceWithANullDefinition_DoesNotEraseTheEarlierOne()
    {
        var real = Catalogues.One("svc-one", new ServiceDefinition { Namespace = "first" });

        var empty = new ServiceDefinitions
        {
            Services = new Dictionary<string, ServiceDefinition>(StringComparer.Ordinal) { ["svc-one"] = null! },
        };

        var definition = await Catalogues.RegistryOver(real, empty).GetDefinition("svc-one");

        Assert.NotNull(definition);
        Assert.Equal("first", definition.Namespace);
    }

    [Fact]
    public void Constructor_NullSources_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new DefaultServiceRegistry(null!));
    }

    [Fact]
    public async Task Constructor_ReadsEverySourceExactlyOnce()
    {
        // Sources are read while the registry is built, not per lookup, which is what makes an
        // expensive source — a file, a mounted volume — cost one read per host.
        var source = new CountingSource();

        var registry = new DefaultServiceRegistry([source]);

        Assert.Equal(1, source.Reads);

        await registry.GetDefinition("svc-one");
        await registry.GetDefinition("svc-one", "istio");

        Assert.Equal(1, source.Reads);
    }

    /// <summary>
    /// A source that counts how many times it was read.
    /// </summary>
    private sealed class CountingSource : IServiceDefinitionsSource
    {
        /// <summary>
        /// How many times the catalogue was asked for.
        /// </summary>
        public int Reads { get; private set; }

        /// <inheritdoc />
        public ServiceDefinitions GetDefinitions()
        {
            this.Reads++;

            return Catalogues.Reference();
        }
    }
}
