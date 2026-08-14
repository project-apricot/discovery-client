using ApricotFramework.DiscoveryClient.Impl;
using ApricotFramework.DiscoveryClient.Model;

namespace ApricotFramework.DiscoveryClient.Tests;

/// <summary>
/// Catalogues the tests resolve against.
/// </summary>
/// <remarks>
/// Built in code rather than parsed, so the core tests exercise resolution alone and do not depend on
/// the YAML package. <see cref="Reference"/> mirrors the catalogue the YAML tests parse from a file,
/// which is what lets both suites share one set of expected URLs.
/// </remarks>
internal static class Catalogues
{
    /// <summary>
    /// The catalogue the golden URL vectors are written against.
    /// </summary>
    /// <remarks>
    /// Two services with an api, ui and grpc port on each of http and https, and an <c>istio</c>
    /// profile on the first that collapses api and grpc onto 443 while saying nothing about ui.
    /// </remarks>
    public static ServiceDefinitions Reference()
    {
        return new ServiceDefinitions
        {
            Services = new Dictionary<string, ServiceDefinition>(StringComparer.Ordinal)
            {
                ["svc-one"] = new()
                {
                    Namespace = "namespace-one",
                    Domain = "new-cluster.cool",
                    Ports = Ports(11100),
                    Overrides = new Dictionary<string, ServiceDefinition>(StringComparer.Ordinal)
                    {
                        ["istio"] = new ServiceDefinition
                        {
                            Ports =
                            [
                                Port("https", ServicePortRoles.Api, 443),
                                Port("https", ServicePortRoles.Grpc, 443),
                            ],
                        },
                    },
                },
                ["svc-two"] = new ServiceDefinition
                {
                    Namespace = "namespace-two",
                    Domain = "new-cluster.cool",
                    Ports = Ports(11106),
                },
            },
        };
    }

    /// <summary>
    /// Builds a registry over one or more catalogues.
    /// </summary>
    /// <param name="catalogues">The catalogues to merge, in order.</param>
    /// <returns>The registry.</returns>
    public static DefaultServiceRegistry RegistryOver(params ServiceDefinitions?[] catalogues)
    {
        return new DefaultServiceRegistry(catalogues.Select(catalogue => new FixedSource(catalogue)));
    }

    /// <summary>
    /// Builds a client over one or more catalogues.
    /// </summary>
    /// <param name="options">The settings to resolve with.</param>
    /// <param name="catalogues">The catalogues to merge, in order.</param>
    /// <returns>The client.</returns>
    public static DefaultStaticDiscoveryClient ClientOver(StaticDiscoveryOptions options, params ServiceDefinitions?[] catalogues)
    {
        return new DefaultStaticDiscoveryClient(RegistryOver(catalogues), options);
    }

    /// <summary>
    /// Builds one catalogue holding a single service.
    /// </summary>
    /// <param name="name">The service name.</param>
    /// <param name="definition">The service's definition.</param>
    /// <returns>The catalogue.</returns>
    public static ServiceDefinitions One(string name, ServiceDefinition definition)
    {
        return new ServiceDefinitions
        {
            Services = new Dictionary<string, ServiceDefinition>(StringComparer.Ordinal) { [name] = definition },
        };
    }

    /// <summary>
    /// Builds one port definition.
    /// </summary>
    /// <param name="protocol">The protocol, or null for a port that declares none.</param>
    /// <param name="role">The role, or null for a port that declares none.</param>
    /// <param name="port">The port number.</param>
    /// <returns>The port definition.</returns>
    public static ServicePortDefinition Port(string? protocol, string? role, int port)
    {
        return new ServicePortDefinition
        {
            Protocol = protocol,
            Roles = role is null ? null : [role],
            Port = port,
        };
    }

    /// <summary>
    /// Builds the six ports the reference services expose, numbered consecutively from a base.
    /// </summary>
    /// <param name="basePort">The port number of the first entry.</param>
    /// <returns>The ports, in catalogue order.</returns>
    private static List<ServicePortDefinition> Ports(int basePort)
    {
        return
        [
            Port("http", ServicePortRoles.Api, basePort),
            Port("https", ServicePortRoles.Api, basePort + 1),
            Port("http", ServicePortRoles.Ui, basePort + 2),
            Port("https", ServicePortRoles.Ui, basePort + 3),
            Port("http", ServicePortRoles.Grpc, basePort + 4),
            Port("https", ServicePortRoles.Grpc, basePort + 5),
        ];
    }

    /// <summary>
    /// A source over a catalogue handed to it, including none at all.
    /// </summary>
    private sealed class FixedSource : IServiceDefinitionsSource
    {
        /// <summary>
        /// The catalogue to supply, or null.
        /// </summary>
        private readonly ServiceDefinitions? catalogue;

        /// <summary>
        /// Creates a new source.
        /// </summary>
        /// <param name="catalogue">The catalogue to supply, or null.</param>
        public FixedSource(ServiceDefinitions? catalogue)
        {
            this.catalogue = catalogue;
        }

        /// <inheritdoc />
        public ServiceDefinitions? GetDefinitions()
        {
            return this.catalogue;
        }
    }
}
