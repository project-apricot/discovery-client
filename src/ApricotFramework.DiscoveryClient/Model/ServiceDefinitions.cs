namespace ApricotFramework.DiscoveryClient.Model;

/// <summary>
/// A catalogue of services, keyed by service name.
/// </summary>
/// <remarks>
/// The key is the name callers pass to the discovery client, and it doubles as the hostname under
/// container and Kubernetes hosting. It is matched case-sensitively.
/// </remarks>
public class ServiceDefinitions
{
    /// <summary>
    /// The services in the catalogue, or <see langword="null"/> for a catalogue that declares none.
    /// </summary>
    public IDictionary<string, ServiceDefinition>? Services { get; set; }
}
