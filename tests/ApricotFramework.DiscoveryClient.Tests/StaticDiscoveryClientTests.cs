using ApricotFramework.DiscoveryClient.Impl;
using ApricotFramework.DiscoveryClient.Model;

namespace ApricotFramework.DiscoveryClient.Tests;

/// <summary>
/// Covers URL resolution: the golden table, the three named accessors, and what happens when a
/// catalogue or a caller supplies something unusable.
/// </summary>
public class StaticDiscoveryClientTests
{
    [Theory]
    [MemberData(nameof(UrlVectors.Cases), MemberType = typeof(UrlVectors))]
    public async Task GetBaseUrl_KnownVector_ReturnsExpectedUrl(
        string? hostingType, string? profile, string? protocol, string? grpcProtocol, string role, string expectedUrl)
    {
        var client = ClientWith(hostingType, profile, protocol, grpcProtocol);

        var resolved = await client.GetBaseUrl(UrlVectors.ServiceOf(expectedUrl), role);

        Assert.Equal(expectedUrl, resolved);
    }

    [Fact]
    public async Task GetApiBase_ResolvesTheApiRole()
    {
        var client = ClientWith("k8s", profile: null, "https", "https");

        Assert.Equal(
            await client.GetBaseUrl("svc-one", ServicePortRoles.Api),
            await client.GetApiBase("svc-one"));
    }

    [Fact]
    public async Task GetGrpcUrl_ResolvesTheGrpcRole()
    {
        var client = ClientWith("k8s", profile: null, "https", "https");

        Assert.Equal(
            await client.GetBaseUrl("svc-one", ServicePortRoles.Grpc),
            await client.GetGrpcUrl("svc-one"));
    }

    [Fact]
    public async Task GetGrpcUrl_UsesTheGrpcProtocolRatherThanTheDefault()
    {
        var client = ClientWith("localhost", profile: null, "https", "http");

        Assert.Equal("https://localhost:11101", await client.GetApiBase("svc-one"));
        Assert.Equal("http://localhost:11104", await client.GetGrpcUrl("svc-one"));
    }

    [Theory]
    [InlineData("orders", "http://localhost:11100/orders")]
    [InlineData("/orders", "http://localhost:11100/orders")]
    [InlineData("orders/", "http://localhost:11100/orders/")]
    [InlineData("/v1/orders/42", "http://localhost:11100/v1/orders/42")]
    [InlineData("orders?status=open", "http://localhost:11100/orders?status=open")]
    [InlineData("", "http://localhost:11100/")]
    public async Task GetApiUrl_JoinsThePathWithASingleSlash(string api, string expectedUrl)
    {
        var client = ClientWith("localhost", profile: null, "http", "http");

        Assert.Equal(expectedUrl, await client.GetApiUrl("svc-one", api));
    }

    [Fact]
    public async Task GetApiUrl_UnknownService_ReturnsNull()
    {
        var client = ClientWith("localhost", profile: null, "http", "http");

        Assert.Null(await client.GetApiUrl("no-such-service", "orders"));
    }

    [Theory]
    [InlineData("no-such-service")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("SVC-ONE")]
    [InlineData("svc one")]
    [InlineData("svc-one:11100")]
    [InlineData("сервис")]
    [InlineData("svc-one/../svc-two")]
    public async Task GetApiBase_ServiceNotInTheCatalogue_ReturnsNull(string service)
    {
        var client = ClientWith("localhost", profile: null, "http", "http");

        Assert.Null(await client.GetApiBase(service));
    }

    [Theory]
    [InlineData("grpc")]
    [InlineData("tcp")]
    [InlineData("ftp")]
    [InlineData("HTTP://")]
    [InlineData("h2")]
    public async Task GetApiBase_ProtocolThatIsNotHttpOrHttps_ReturnsNull(string protocol)
    {
        var client = ClientWith("localhost", profile: null, protocol, "http");

        Assert.Null(await client.GetApiBase("svc-one"));
    }

    [Theory]
    [InlineData("http")]
    [InlineData("HTTP")]
    [InlineData("Http")]
    public async Task GetApiBase_ProtocolCasing_ResolvesTheSameUrl(string protocol)
    {
        var client = ClientWith("localhost", profile: null, protocol, "http");

        Assert.Equal("http://localhost:11100", await client.GetApiBase("svc-one"));
    }

    [Theory]
    [InlineData("HTTPS")]
    [InlineData("Https")]
    public async Task GetApiBase_CatalogueProtocolCasing_StillMatches(string catalogueProtocol)
    {
        // A catalogue may spell its protocols either way; the resolver folds case on both sides. The
        // scheme in the URL is the canonical lower-case form regardless.
        var catalogue = Catalogues.One("svc-one", new ServiceDefinition
        {
            Ports = [Catalogues.Port(catalogueProtocol, ServicePortRoles.Api, 8443)],
        });

        var client = Catalogues.ClientOver(
            new StaticDiscoveryOptions { HostingType = StaticHostingTypes.Localhost, DefaultProtocol = "https" },
            catalogue);

        Assert.Equal("https://localhost:8443", await client.GetApiBase("svc-one"));
    }

    [Fact]
    public async Task GetApiBase_RoleCasing_DoesNotMatch()
    {
        // Roles are matched case-sensitively, unlike protocols: a role is a label a catalogue invents,
        // so there is no canonical spelling to fold to.
        var catalogue = Catalogues.One("svc-one", new ServiceDefinition
        {
            Ports = [Catalogues.Port("http", "API", 8080)],
        });

        var client = Catalogues.ClientOver(new StaticDiscoveryOptions(), catalogue);

        Assert.Null(await client.GetApiBase("svc-one"));
    }

    [Fact]
    public async Task GetBaseUrl_RoleTheLibraryDoesNotName_Resolves()
    {
        var catalogue = Catalogues.One("svc-one", new ServiceDefinition
        {
            Ports = [Catalogues.Port("http", "metrics", 9090)],
        });

        var client = Catalogues.ClientOver(new StaticDiscoveryOptions(), catalogue);

        Assert.Equal("http://svc-one:9090", await client.GetBaseUrl("svc-one", "metrics"));
    }

    [Fact]
    public async Task GetApiBase_UnrecognisedHostingType_ResolvesToTheServiceName()
    {
        var client = ClientWith("mainframe", profile: null, "http", "http");

        Assert.Equal("http://svc-one:11100", await client.GetApiBase("svc-one"));
    }

    [Fact]
    public async Task GetApiBase_KubernetesWithNoNamespaceOrDomain_ResolvesToTheBareServiceName()
    {
        // Inside a cluster the pod's own DNS search domains complete this, which is what a caller in
        // the same namespace wants.
        var catalogue = Catalogues.One("svc-one", new ServiceDefinition
        {
            Ports = [Catalogues.Port("http", ServicePortRoles.Api, 8080)],
        });

        var client = Catalogues.ClientOver(
            new StaticDiscoveryOptions { HostingType = StaticHostingTypes.K8s },
            catalogue);

        Assert.Equal("http://svc-one:8080", await client.GetApiBase("svc-one"));
    }

    [Theory]
    [InlineData("team-a", null, "svc-one.team-a.svc.cluster.local")]
    [InlineData(null, "other.cluster", "svc-one.default.svc.other.cluster")]
    [InlineData("team-a", "other.cluster", "svc-one.team-a.svc.other.cluster")]
    public async Task GetApiBase_KubernetesFillsTheMissingHalfFromItsDefault(string? ns, string? domain, string expectedHost)
    {
        var catalogue = Catalogues.One("svc-one", new ServiceDefinition
        {
            Namespace = ns,
            Domain = domain,
            Ports = [Catalogues.Port("http", ServicePortRoles.Api, 8080)],
        });

        var client = Catalogues.ClientOver(
            new StaticDiscoveryOptions { HostingType = StaticHostingTypes.K8s },
            catalogue);

        Assert.Equal($"http://{expectedHost}:8080", await client.GetApiBase("svc-one"));
    }

    [Fact]
    public async Task GetApiBase_NoDefaultProtocol_FallsBackToHttp()
    {
        var client = Catalogues.ClientOver(new StaticDiscoveryOptions(), Catalogues.Reference());

        Assert.Equal("http://svc-one:11100", await client.GetApiBase("svc-one"));
    }

    [Fact]
    public async Task GetApiBase_ProfileTheServiceDoesNotDeclare_ResolvesAsWritten()
    {
        var client = ClientWith("localhost", "no-such-profile", "https", "https");

        Assert.Equal("https://localhost:11101", await client.GetApiBase("svc-one"));
    }

    [Fact]
    public async Task GetApiBase_ProfileMatchedCaseSensitively()
    {
        var client = ClientWith("localhost", "ISTIO", "https", "https");

        Assert.Equal("https://localhost:11101", await client.GetApiBase("svc-one"));
    }

    [Fact]
    public async Task GetApiBase_ServiceWithNoPorts_ReturnsNull()
    {
        var catalogue = Catalogues.One("svc-one", new ServiceDefinition { Ports = null });

        var client = Catalogues.ClientOver(new StaticDiscoveryOptions(), catalogue);

        Assert.Null(await client.GetApiBase("svc-one"));
    }

    [Fact]
    public async Task GetApiBase_ServiceWithNoRolesOnItsPort_ReturnsNull()
    {
        var catalogue = Catalogues.One("svc-one", new ServiceDefinition
        {
            Ports = [Catalogues.Port("http", role: null, 8080)],
        });

        var client = Catalogues.ClientOver(new StaticDiscoveryOptions(), catalogue);

        Assert.Null(await client.GetApiBase("svc-one"));
    }

    [Fact]
    public async Task GetApiBase_ServiceWithNoProtocolOnItsPort_ReturnsNull()
    {
        var catalogue = Catalogues.One("svc-one", new ServiceDefinition
        {
            Ports = [Catalogues.Port(protocol: null, ServicePortRoles.Api, 8080)],
        });

        var client = Catalogues.ClientOver(new StaticDiscoveryOptions(), catalogue);

        Assert.Null(await client.GetApiBase("svc-one"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public async Task GetApiBase_AbsurdPortNumber_IsReturnedAsWritten(int port)
    {
        // The library does not police port ranges: the catalogue is the operator's statement of fact,
        // and rejecting a value here would only move the error further from the file that has it.
        var catalogue = Catalogues.One("svc-one", new ServiceDefinition
        {
            Ports = [Catalogues.Port("http", ServicePortRoles.Api, port)],
        });

        var client = Catalogues.ClientOver(new StaticDiscoveryOptions(), catalogue);

        Assert.Equal($"http://svc-one:{port}", await client.GetApiBase("svc-one"));
    }

    [Fact]
    public async Task GetApiBase_EmptyCatalogue_ReturnsNull()
    {
        var client = new DefaultStaticDiscoveryClient(
            new DefaultServiceRegistry([new EmptyServiceDefinitionsSource()]),
            new StaticDiscoveryOptions());

        Assert.Null(await client.GetApiBase("svc-one"));
    }

    [Fact]
    public async Task GetApiBase_NoSourcesAtAll_ReturnsNull()
    {
        var client = new DefaultStaticDiscoveryClient(new DefaultServiceRegistry([]), new StaticDiscoveryOptions());

        Assert.Null(await client.GetApiBase("svc-one"));
    }

    [Fact]
    public void Constructor_NullArgument_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new DefaultStaticDiscoveryClient(null!, new StaticDiscoveryOptions()));
        Assert.Throws<ArgumentNullException>(() => new DefaultStaticDiscoveryClient(Catalogues.RegistryOver(), null!));
    }

    /// <summary>
    /// Builds a client over the reference catalogue with the given settings.
    /// </summary>
    /// <param name="hostingType">The hosting type.</param>
    /// <param name="profile">The profile.</param>
    /// <param name="protocol">The default protocol.</param>
    /// <param name="grpcProtocol">The default gRPC protocol.</param>
    /// <returns>The client.</returns>
    private static DefaultStaticDiscoveryClient ClientWith(string? hostingType, string? profile, string? protocol, string? grpcProtocol)
    {
        return Catalogues.ClientOver(
            new StaticDiscoveryOptions
            {
                HostingType = hostingType,
                Profile = profile,
                DefaultProtocol = protocol,
                DefaultGrpcProtocol = grpcProtocol,
            },
            Catalogues.Reference());
    }
}
