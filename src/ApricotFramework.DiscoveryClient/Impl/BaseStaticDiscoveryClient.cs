using ApricotFramework.DiscoveryClient.Model;

namespace ApricotFramework.DiscoveryClient.Impl;

/// <summary>
/// Builds URLs from a registry's definitions, leaving where the settings come from to a subclass.
/// </summary>
/// <remarks>
/// All the URL building lives here; the only thing a subclass must supply is the settings in force
/// for a call, which is what separates a client configured once from one that follows configuration
/// reloads.
/// </remarks>
public abstract class BaseStaticDiscoveryClient : IDiscoveryClient
{
    /// <summary>
    /// The cluster domain assumed when a Kubernetes definition names a namespace but no domain.
    /// </summary>
    /// <remarks>
    /// The default every Kubernetes installation uses unless it was explicitly changed at cluster
    /// creation.
    /// </remarks>
    private const string DefaultKubernetesClusterDomain = "cluster.local";

    /// <summary>
    /// The namespace assumed when a Kubernetes definition names a domain but no namespace.
    /// </summary>
    private const string DefaultKubernetesNamespace = "default";

    /// <summary>
    /// The address that resolves to the container host from inside a container.
    /// </summary>
    private const string DockerInternalHost = "host.docker.internal";

    /// <summary>
    /// The loopback hostname.
    /// </summary>
    private const string LoopbackHost = "localhost";

    /// <summary>
    /// The registry supplying definitions.
    /// </summary>
    private readonly IServiceRegistry serviceRegistry;

    /// <summary>
    /// Creates a new client over a registry.
    /// </summary>
    /// <param name="serviceRegistry">The registry supplying definitions.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="serviceRegistry"/> is null.
    /// </exception>
    protected BaseStaticDiscoveryClient(IServiceRegistry serviceRegistry)
    {
        ArgumentNullException.ThrowIfNull(serviceRegistry);

        this.serviceRegistry = serviceRegistry;
    }

    /// <inheritdoc />
    public virtual ValueTask<string?> GetApiBase(string service)
    {
        return this.GetBaseUrl(service, ServicePortRoles.Api);
    }

    /// <inheritdoc />
    public virtual ValueTask<string?> GetApiUrl(string service, string api)
    {
        return this.GetUrl(service, ServicePortRoles.Api, api);
    }

    /// <inheritdoc />
    public virtual ValueTask<string?> GetGrpcUrl(string service)
    {
        return this.GetBaseUrl(service, ServicePortRoles.Grpc);
    }

    /// <inheritdoc />
    public virtual async ValueTask<string?> GetBaseUrl(string service, string role)
    {
        var options = this.GetCurrentOptions();

        var definition = await this.serviceRegistry.GetDefinition(service, options.Profile).ConfigureAwait(false);

        if (definition is null)
        {
            return null;
        }

        // Nothing else can produce a URL, so an unusable protocol setting is indistinguishable from
        // an unknown service here. The ASP.NET Core package rejects it at startup instead.
        var scheme = ResolveScheme(GetConfiguredProtocol(role, options));

        if (scheme is null)
        {
            return null;
        }

        var port = FindPort(definition, scheme, role);

        if (port is null)
        {
            return null;
        }

        var host = this.GetHostname(service, definition, options);

        return $"{scheme}://{host}:{port.Port}";
    }

    /// <inheritdoc />
    public virtual async ValueTask<string?> GetUrl(string service, string role, string? path)
    {
        // A null path is treated as "no URL" rather than an argument error, so a caller passing one
        // through from configuration gets the same negative result as an unknown service.
        if (path is null)
        {
            return null;
        }

        var baseUrl = await this.GetBaseUrl(service, role).ConfigureAwait(false);

        if (baseUrl is null)
        {
            return null;
        }

        // Trimmed on both sides so exactly one slash joins them, whichever way each was written.
        return $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
    }

    /// <summary>
    /// Gets the settings in force for this call.
    /// </summary>
    /// <returns>The current settings.</returns>
    protected abstract StaticDiscoveryOptions GetCurrentOptions();

    /// <summary>
    /// Gets the hostname a service is reached at under the configured hosting type.
    /// </summary>
    /// <param name="service">The service name.</param>
    /// <param name="definition">The service's effective definition.</param>
    /// <param name="options">The settings in force.</param>
    /// <returns>
    /// The hostname, or <see langword="null"/> if the service cannot be addressed at all.
    /// </returns>
    /// <remarks>
    /// Override this to add a hosting type of your own, delegating to <c>base</c> for the ones you do
    /// not handle. An unrecognised hosting type resolves to the service's own name, which is correct
    /// on any network where service names resolve.
    /// </remarks>
    protected virtual string GetHostname(string service, IServiceDefinition definition, StaticDiscoveryOptions options)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(options);

        return options.HostingType switch
        {
            StaticHostingTypes.K8s => GetKubernetesHostname(service, definition),
            StaticHostingTypes.Localhost => LoopbackHost,
            StaticHostingTypes.DockerLocalhost => DockerInternalHost,

            // Listed rather than left to the fallback because it is a supported value, not an
            // unrecognised one that happens to want the same answer.
            StaticHostingTypes.Docker => service,
            _ => service
        };
    }

    /// <summary>
    /// Gets the cluster DNS name a service is reached at inside Kubernetes.
    /// </summary>
    /// <param name="service">The service name, which is also the Kubernetes service name.</param>
    /// <param name="definition">The service's effective definition.</param>
    /// <returns>The hostname to use.</returns>
    /// <remarks>
    /// A definition naming neither a namespace nor a domain resolves to the bare service name, which
    /// inside a cluster the pod's own search domains complete — the right answer for a caller in the
    /// same namespace. Naming either one produces the fully qualified
    /// <c>service.namespace.svc.cluster-domain</c> form, filling the other from its default.
    /// </remarks>
    private static string GetKubernetesHostname(string service, IServiceDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(definition.Domain) && string.IsNullOrWhiteSpace(definition.Namespace))
        {
            return service;
        }

        var clusterDomain = string.IsNullOrWhiteSpace(definition.Domain)
            ? DefaultKubernetesClusterDomain
            : definition.Domain;

        var serviceNamespace = string.IsNullOrWhiteSpace(definition.Namespace)
            ? DefaultKubernetesNamespace
            : definition.Namespace;

        return $"{service}.{serviceNamespace}.svc.{clusterDomain}";
    }

    /// <summary>
    /// Gets the protocol configured for a role.
    /// </summary>
    /// <param name="role">The role being resolved.</param>
    /// <param name="options">The settings in force.</param>
    /// <returns>The configured protocol, which may be null or unusable.</returns>
    private static string? GetConfiguredProtocol(string role, StaticDiscoveryOptions options)
    {
        return string.Equals(role, ServicePortRoles.Grpc, StringComparison.Ordinal)
            ? options.DefaultGrpcProtocol
            : options.DefaultProtocol;
    }

    /// <summary>
    /// Gets the URL scheme a configured protocol maps to.
    /// </summary>
    /// <param name="configured">The configured protocol, if any.</param>
    /// <returns>
    /// The canonical scheme, or <see langword="null"/> if the protocol cannot appear in a URL.
    /// </returns>
    /// <remarks>
    /// Returns the canonical lower-case form rather than what was configured, because a scheme is
    /// lower-case by definition and a catalogue may spell its protocols either way.
    /// </remarks>
    private static string? ResolveScheme(string? configured)
    {
        if (string.IsNullOrWhiteSpace(configured))
        {
            return ServiceProtocolTypes.Http;
        }

        if (string.Equals(configured, ServiceProtocolTypes.Http, StringComparison.OrdinalIgnoreCase))
        {
            return ServiceProtocolTypes.Http;
        }

        return string.Equals(configured, ServiceProtocolTypes.Https, StringComparison.OrdinalIgnoreCase)
            ? ServiceProtocolTypes.Https
            : null;
    }

    /// <summary>
    /// Finds the port serving a role over a scheme.
    /// </summary>
    /// <param name="definition">The service's effective definition.</param>
    /// <param name="scheme">The scheme to match.</param>
    /// <param name="role">The role to match.</param>
    /// <returns>The matching port, or <see langword="null"/> if there is none.</returns>
    /// <remarks>
    /// The <em>last</em> match wins, and that is load-bearing rather than arbitrary: a registry
    /// appends a profile's ports after the ones written on the service, so taking the last match is
    /// what makes the profile override rather than be shadowed.
    /// </remarks>
    private static ServicePortDefinition? FindPort(IServiceDefinition definition, string scheme, string role)
    {
        return (definition.Ports ?? []).LastOrDefault(port =>
            string.Equals(scheme, port.Protocol, StringComparison.OrdinalIgnoreCase)
            && port.Roles?.Contains(role, StringComparer.Ordinal) == true);
    }
}
