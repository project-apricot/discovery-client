namespace ApricotFramework.DiscoveryClient;

/// <summary>
/// Resolves a service name to a URL a caller can send a request to.
/// </summary>
/// <remarks>
/// Every method answers <see langword="null"/> rather than throwing when a service, a role or a
/// protocol cannot be resolved. A caller that cannot proceed without the URL should say so itself,
/// with the context to make the message useful.
/// </remarks>
public interface IDiscoveryClient
{
    /// <summary>
    /// Gets the base URL of a service's HTTP API.
    /// </summary>
    /// <param name="service">The service name.</param>
    /// <returns>
    /// The base URL — scheme, host and port, with no trailing slash — or <see langword="null"/> if
    /// the service is unknown or exposes no matching port.
    /// </returns>
    ValueTask<string?> GetApiBase(string service);

    /// <summary>
    /// Gets the URL of one API path on a service.
    /// </summary>
    /// <param name="service">The service name.</param>
    /// <param name="api">The path to append, with or without a leading slash.</param>
    /// <returns>
    /// The absolute URL, or <see langword="null"/> if the service cannot be resolved or
    /// <paramref name="api"/> is <see langword="null"/>.
    /// </returns>
    ValueTask<string?> GetApiUrl(string service, string api);

    /// <summary>
    /// Gets the base URL of a service's gRPC endpoint.
    /// </summary>
    /// <param name="service">The service name.</param>
    /// <returns>
    /// The base URL, or <see langword="null"/> if the service is unknown or exposes no matching
    /// port.
    /// </returns>
    /// <remarks>
    /// Uses the gRPC default protocol, which is configured separately: gRPC and the HTTP API often
    /// differ on whether TLS terminates at the service or before it.
    /// </remarks>
    ValueTask<string?> GetGrpcUrl(string service);

    /// <summary>
    /// Gets the base URL of whichever port serves a given role.
    /// </summary>
    /// <param name="service">The service name.</param>
    /// <param name="role">The role to resolve — see <see cref="Model.ServicePortRoles"/>.</param>
    /// <returns>
    /// The base URL, or <see langword="null"/> if the service is unknown or exposes no port with
    /// that role on the applicable protocol.
    /// </returns>
    /// <remarks>
    /// The general form behind the other methods, and the only way to reach a role they do not name
    /// — <see cref="Model.ServicePortRoles.Ui"/>, or one a catalogue invents.
    /// </remarks>
    ValueTask<string?> GetBaseUrl(string service, string role);

    /// <summary>
    /// Gets the URL of one path on whichever port serves a given role.
    /// </summary>
    /// <param name="service">The service name.</param>
    /// <param name="role">The role to resolve — see <see cref="Model.ServicePortRoles"/>.</param>
    /// <param name="path">The path to append, with or without a leading slash.</param>
    /// <returns>
    /// The absolute URL, or <see langword="null"/> if the service cannot be resolved or
    /// <paramref name="path"/> is <see langword="null"/>.
    /// </returns>
    ValueTask<string?> GetUrl(string service, string role, string path);
}
