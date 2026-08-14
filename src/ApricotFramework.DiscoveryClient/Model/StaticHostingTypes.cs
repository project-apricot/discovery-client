namespace ApricotFramework.DiscoveryClient.Model;

/// <summary>
/// The hosting environments a service can be resolved for.
/// </summary>
/// <remarks>
/// The hosting type decides the hostname half of a resolved URL; the catalogue decides the port
/// half. That split is what lets one catalogue serve a developer laptop and a cluster unchanged.
/// The values are the strings written into configuration, so they are a stored contract and do not
/// change.
/// </remarks>
public static class StaticHostingTypes
{
    /// <summary>
    /// Every service runs on the local machine, reached as <c>localhost</c> on its own port.
    /// </summary>
    public const string Localhost = "localhost";

    /// <summary>
    /// Every service runs in a container on a shared network, reached by its service name.
    /// </summary>
    public const string Docker = "docker";

    /// <summary>
    /// Services run both in containers and on the host, reached as <c>host.docker.internal</c> so
    /// callers inside and outside a container use the same address.
    /// </summary>
    /// <remarks>
    /// This requires the host machine to resolve <c>host.docker.internal</c> to itself, which
    /// Docker Desktop arranges but a bare Linux Docker installation does not.
    /// </remarks>
    public const string DockerLocalhost = "docker_localhost";

    /// <summary>
    /// Services run in a Kubernetes cluster, reached by the cluster DNS name built from the
    /// service's namespace and cluster domain.
    /// </summary>
    public const string K8s = "k8s";
}
