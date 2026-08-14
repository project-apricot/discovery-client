using System.Text;
using ApricotFramework.DiscoveryClient.Model;

namespace ApricotFramework.DiscoveryClient.Yaml.Tests;

/// <summary>
/// Covers what a YAML catalogue deserializes into, and what happens to one that is malformed,
/// incomplete, or carries keys the model does not know.
/// </summary>
public class YamlServiceDefinitionsSourceTests
{
    /// <summary>
    /// The catalogue file the object-graph assertions are written against.
    /// </summary>
    /// <remarks>
    /// The expected graph below was taken from parsing this file with <c>yq</c>, a Go implementation of
    /// YAML with no code in common with YamlDotNet — so the assertions pin the file's meaning rather
    /// than one parser's reading of it.
    /// </remarks>
    private const string ReferenceCatalogue = "Catalogues.services.yaml";

    [Fact]
    public void GetDefinitions_ReferenceCatalogue_DeserializesTheExpectedGraph()
    {
        var catalogue = ReadReference();

        Assert.NotNull(catalogue.Services);
        Assert.Equal(["svc-one", "svc-two"], catalogue.Services.Keys.Order(StringComparer.Ordinal));

        var one = catalogue.Services["svc-one"];

        Assert.Equal("namespace-one", one.Namespace);
        Assert.Equal("new-cluster.cool", one.Domain);

        var ports = one.Ports!.ToList();

        Assert.Equal(6, ports.Count);
        Assert.Equal(
            [("http", "api", 11100), ("https", "api", 11101), ("http", "ui", 11102),
             ("https", "ui", 11103), ("http", "grpc", 11104), ("https", "grpc", 11105)],
            ports.Select(port => (port.Protocol, port.Roles!.Single(), port.Port)));

        Assert.NotNull(one.Overrides);
        Assert.Equal(["istio"], one.Overrides.Keys);
        Assert.Equal(
            [("https", "api", 443), ("https", "grpc", 443)],
            one.Overrides["istio"].Ports!.Select(port => (port.Protocol, port.Roles!.Single(), port.Port)));

        var two = catalogue.Services["svc-two"];

        Assert.Equal("namespace-two", two.Namespace);
        Assert.Equal(11106, two.Ports!.First().Port);
        Assert.Null(two.Overrides);
    }

    [Theory]
    [InlineData("")]
    [InlineData("\n")]
    [InlineData("# only a comment\n")]
    [InlineData("--- \n")]
    public void GetDefinitions_EmptyDocument_ReturnsACatalogueWithNoServices(string yaml)
    {
        // An operator who mounted an empty file gets no services, not a host that will not start.
        var catalogue = Parse(yaml);

        Assert.NotNull(catalogue);
        Assert.True(catalogue.Services is null || catalogue.Services.Count == 0);
    }

    [Fact]
    public void GetDefinitions_ServicesKeyWithNoValue_ReturnsNoServices()
    {
        var catalogue = Parse("services:\n");

        Assert.NotNull(catalogue);
        Assert.Null(catalogue.Services);
    }

    [Fact]
    public void GetDefinitions_KeyTheModelDoesNotKnow_IsIgnored()
    {
        // A catalogue can carry annotations, or a field a newer version of this library reads, without
        // stopping a host that does not understand it.
        var catalogue = Parse("""
            owner: platform-team
            services:
              svc-one:
                namespace: team-a
                description: the first service
                ports:
                  - protocol: http
                    roles: [ 'api' ]
                    port: 8080
                    comment: the public one
            """);

        Assert.NotNull(catalogue.Services);
        Assert.Equal("team-a", catalogue.Services["svc-one"].Namespace);
        Assert.Equal(8080, catalogue.Services["svc-one"].Ports!.Single().Port);
    }

    [Fact]
    public void GetDefinitions_MisspelledKey_IsSilentlySkipped()
    {
        // The cost of tolerating unknown keys: `port:` for `ports:` is skipped rather than reported,
        // and the service resolves to nothing.
        var catalogue = Parse("""
            services:
              svc-one:
                port:
                  - protocol: http
                    roles: [ 'api' ]
                    port: 8080
            """);

        Assert.NotNull(catalogue.Services);
        Assert.Null(catalogue.Services["svc-one"].Ports);
    }

    [Fact]
    public void GetDefinitions_ServiceWithNoBody_DeserializesToANullDefinition()
    {
        // What YAML says a body-less entry means, and worth pinning because it is the one place a
        // catalogue can put a null into the service map. The registry drops such entries.
        var catalogue = Parse("services:\n  svc-one:\n");

        Assert.NotNull(catalogue.Services);
        Assert.True(catalogue.Services.ContainsKey("svc-one"));
        Assert.Null(catalogue.Services["svc-one"]);
    }

    [Fact]
    public void GetDefinitions_ServiceWithAnEmptyMapping_DeserializesToAnEmptyDefinition()
    {
        // Unlike a body-less entry, an explicit empty mapping is a definition — one that declares
        // nothing. The documentation uses this form to show the Kubernetes hostname defaults.
        var catalogue = Parse("services:\n  svc-one: { }\n");

        var definition = catalogue.Services!["svc-one"];

        Assert.NotNull(definition);
        Assert.Null(definition.Namespace);
        Assert.Null(definition.Domain);
        Assert.Null(definition.Ports);
    }

    [Fact]
    public void GetDefinitions_InlineMappingsForPortsAndService_AreRead()
    {
        // The compact form the documentation uses for its examples.
        var catalogue = Parse("""
            services:
              svc-one:
                namespace: payments
                ports:
                  - { protocol: https, roles: [ 'api', 'ui' ], port: 11101 }
            """);

        var port = catalogue.Services!["svc-one"].Ports!.Single();

        Assert.Equal("payments", catalogue.Services["svc-one"].Namespace);
        Assert.Equal("https", port.Protocol);
        Assert.Equal(["api", "ui"], port.Roles);
        Assert.Equal(11101, port.Port);
    }

    [Theory]
    [InlineData("http")]
    [InlineData("HTTPS")]
    [InlineData("Http")]
    public void GetDefinitions_ProtocolCasing_IsPreservedAsWritten(string protocol)
    {
        // The parser stores what the file says; folding case is the resolver's job.
        var catalogue = Parse($"""
            services:
              svc-one:
                ports:
                  - protocol: {protocol}
                    roles: [ 'api' ]
                    port: 8080
            """);

        Assert.Equal(protocol, catalogue.Services!["svc-one"].Ports!.Single().Protocol);
    }

    [Fact]
    public void GetDefinitions_MultipleRolesOnOnePort_AreAllRead()
    {
        var catalogue = Parse("""
            services:
              svc-one:
                ports:
                  - protocol: http
                    roles: [ 'api', 'ui', 'grpc' ]
                    port: 8080
            """);

        Assert.Equal(["api", "ui", "grpc"], catalogue.Services!["svc-one"].Ports!.Single().Roles);
    }

    [Fact]
    public void GetDefinitions_ServiceNameWithUnusualCharacters_IsReadVerbatim()
    {
        var catalogue = Parse("""
            services:
              "svc.with:delimiters/and space":
                ports:
                  - protocol: http
                    roles: [ 'api' ]
                    port: 8080
              сервис:
                namespace: команда
            """);

        Assert.NotNull(catalogue.Services);
        Assert.True(catalogue.Services.ContainsKey("svc.with:delimiters/and space"));
        Assert.Equal("команда", catalogue.Services["сервис"].Namespace);
    }

    [Fact]
    public void GetDefinitions_ServiceNamesDifferingOnlyInCase_AreDistinct()
    {
        // Service names are matched case-sensitively throughout, so these are two services.
        var catalogue = Parse("services:\n  svc-one:\n  SVC-ONE:\n");

        Assert.Equal(2, catalogue.Services!.Count);
    }

    [Fact]
    public void GetDefinitions_MalformedYaml_Throws()
    {
        // A catalogue that is not YAML at all is a deployment mistake, not a service that cannot be
        // found, so it is raised rather than swallowed.
        Assert.ThrowsAny<Exception>(() => Parse("services:\n  svc-one:\n   - this: is not a mapping\n"));
    }

    [Fact]
    public void GetDefinitions_PortThatIsNotANumber_Throws()
    {
        Assert.ThrowsAny<Exception>(() => Parse("""
            services:
              svc-one:
                ports:
                  - protocol: http
                    roles: [ 'api' ]
                    port: not-a-number
            """));
    }

    [Fact]
    public void GetDefinitions_ReadsTheStreamAfreshOnEveryCall()
    {
        var reads = 0;

        var source = new YamlServiceDefinitionsSource(() =>
        {
            reads++;

            return new MemoryStream(Encoding.UTF8.GetBytes("services:\n  svc-one:\n"));
        });

        source.GetDefinitions();
        source.GetDefinitions();

        Assert.Equal(2, reads);
    }

    [Fact]
    public void GetDefinitions_SupplierReturningNoStream_Throws()
    {
        var source = new YamlServiceDefinitionsSource(() => null);

        Assert.Throws<InvalidOperationException>(() => source.GetDefinitions());
    }

    [Fact]
    public void Constructor_NullSupplier_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new YamlServiceDefinitionsSource(null!));
    }

    /// <summary>
    /// Parses a catalogue written inline.
    /// </summary>
    /// <param name="yaml">The catalogue text.</param>
    /// <returns>The parsed catalogue.</returns>
    private static ServiceDefinitions Parse(string yaml)
    {
        var source = new YamlServiceDefinitionsSource(() => new MemoryStream(Encoding.UTF8.GetBytes(yaml)));

        return Assert.IsType<ServiceDefinitions>(source.GetDefinitions());
    }

    /// <summary>
    /// Parses the reference catalogue from its embedded copy.
    /// </summary>
    /// <returns>The parsed catalogue.</returns>
    private static ServiceDefinitions ReadReference()
    {
        var source = new EmbeddedYamlServiceDefinitionsSource(ReferenceCatalogue, typeof(YamlServiceDefinitionsSourceTests).Assembly);

        return Assert.IsType<ServiceDefinitions>(source.GetDefinitions());
    }
}
