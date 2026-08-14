namespace ApricotFramework.DiscoveryClient.Tests;

/// <summary>
/// The URLs the resolver must produce, as a fixed table.
/// </summary>
/// <remarks>
/// These are the library's observable contract: a caller stores them in configuration, logs them, and
/// compares them. Changing any expected value here changes where a consumer's traffic goes, so a
/// failure means the resolver moved, not that the table is stale.
/// <para>
/// The values were not read off this implementation. The catalogue was parsed with <c>yq</c>, an
/// independent YAML implementation, and the ports picked from that parse by the documented rule (last
/// entry matching the protocol and role, with a profile's ports appended after the service's own).
/// The Kubernetes hostnames follow the Kubernetes DNS specification's
/// <c>service.namespace.svc.cluster-domain</c> form rather than anything this code computes.
/// </para>
/// </remarks>
public static class UrlVectors
{
    /// <summary>
    /// Hosting type, profile, default protocol, default gRPC protocol, role, and the expected URL.
    /// </summary>
    public static TheoryData<string?, string?, string?, string?, string, string> Cases =>
        new()
        {
            // Kubernetes behind an ingress that terminates TLS: the istio profile collapses api and
            // grpc onto 443, and leaves ui — which it does not mention — on the service's own port.
            { "k8s", "istio", "https", "https", "api", "https://svc-one.namespace-one.svc.new-cluster.cool:443" },
            { "k8s", "istio", "https", "https", "ui", "https://svc-one.namespace-one.svc.new-cluster.cool:11103" },
            { "k8s", "istio", "https", "https", "grpc", "https://svc-one.namespace-one.svc.new-cluster.cool:443" },

            // The same cluster with no profile applied: every role on its own declared port.
            { "k8s", null, "https", "https", "api", "https://svc-one.namespace-one.svc.new-cluster.cool:11101" },
            { "k8s", null, "https", "https", "ui", "https://svc-one.namespace-one.svc.new-cluster.cool:11103" },
            { "k8s", null, "https", "https", "grpc", "https://svc-one.namespace-one.svc.new-cluster.cool:11105" },
            { "k8s", null, "http", "http", "api", "http://svc-one.namespace-one.svc.new-cluster.cool:11100" },
            { "k8s", null, "http", "http", "ui", "http://svc-one.namespace-one.svc.new-cluster.cool:11102" },
            { "k8s", null, "http", "http", "grpc", "http://svc-one.namespace-one.svc.new-cluster.cool:11104" },

            // A developer machine: one host, ports telling the services apart.
            { "localhost", null, "http", "http", "api", "http://localhost:11100" },
            { "localhost", null, "http", "http", "ui", "http://localhost:11102" },
            { "localhost", null, "http", "http", "grpc", "http://localhost:11104" },

            // A container network: the service name is the hostname.
            { "docker", null, "http", "http", "api", "http://svc-one:11100" },
            { "docker", null, "http", "http", "ui", "http://svc-one:11102" },
            { "docker", null, "http", "http", "grpc", "http://svc-one:11104" },

            // Containers and host processes side by side, with gRPC left on plain HTTP — the case the
            // separate gRPC protocol setting exists for.
            { "docker_localhost", null, "https", "http", "api", "https://host.docker.internal:11101" },
            { "docker_localhost", null, "https", "http", "ui", "https://host.docker.internal:11103" },
            { "docker_localhost", null, "https", "http", "grpc", "http://host.docker.internal:11104" },

            // No hosting type at all still resolves, to the service's own name.
            { null, "istio", "https", "https", "api", "https://svc-one:443" },
            { null, "istio", "https", "https", "ui", "https://svc-one:11103" },
            { null, "istio", "https", "https", "grpc", "https://svc-one:443" },

            // The second service, to prove the ports are read per service and not from the first one.
            { "k8s", null, "https", "https", "api", "https://svc-two.namespace-two.svc.new-cluster.cool:11107" },
        };

    /// <summary>
    /// The service each case resolves, which is the second one only for the final case.
    /// </summary>
    /// <param name="expectedUrl">The expected URL of the case.</param>
    /// <returns>The service name to resolve.</returns>
    public static string ServiceOf(string expectedUrl)
    {
        return expectedUrl.Contains("svc-two", StringComparison.Ordinal) ? "svc-two" : "svc-one";
    }
}
