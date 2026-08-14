namespace ApricotFramework.DiscoveryClient.Model;

/// <summary>
/// What a service registry knows about one service.
/// </summary>
/// <remarks>
/// This is the read side of a definition: what a discovery client needs to build a URL, with the
/// profile overrides already applied.
/// </remarks>
public interface IServiceDefinition
{
    /// <summary>
    /// The Kubernetes namespace the service runs in, or <see langword="null"/> to leave it to the
    /// resolver's default.
    /// </summary>
    /// <remarks>
    /// Only consulted when resolving for Kubernetes hosting.
    /// </remarks>
    string? Namespace { get; }

    /// <summary>
    /// The cluster domain to qualify the service name with, or <see langword="null"/> to leave it to
    /// the resolver's default.
    /// </summary>
    /// <remarks>
    /// Only consulted when resolving for Kubernetes hosting.
    /// </remarks>
    string? Domain { get; }

    /// <summary>
    /// The ports the service listens on.
    /// </summary>
    /// <remarks>
    /// Order matters: when two entries match the same protocol and role, the later one wins. That is
    /// how a profile override takes precedence, since overriding ports are appended after the base
    /// ones.
    /// </remarks>
    IEnumerable<ServicePortDefinition>? Ports { get; }
}
