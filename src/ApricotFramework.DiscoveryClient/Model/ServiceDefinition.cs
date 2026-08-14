namespace ApricotFramework.DiscoveryClient.Model;

/// <summary>
/// One service as written in a catalogue, including its per-profile overrides.
/// </summary>
/// <remarks>
/// This is the write side of a definition: the shape a catalogue file deserializes into, which is
/// why every member is settable and every collection may be absent. Resolving it against a profile
/// yields an <see cref="EffectiveServiceDefinition"/>.
/// </remarks>
public class ServiceDefinition : IServiceDefinition
{
    /// <inheritdoc cref="IServiceDefinition.Namespace" />
    public string? Namespace { get; set; }

    /// <inheritdoc cref="IServiceDefinition.Domain" />
    public string? Domain { get; set; }

    /// <inheritdoc cref="IServiceDefinition.Ports" />
    public IEnumerable<ServicePortDefinition>? Ports { get; set; }

    /// <summary>
    /// Values that replace or extend the ones above, keyed by profile name.
    /// </summary>
    /// <remarks>
    /// A profile stands for a deployment variant of the same topology — an ingress that terminates
    /// TLS and collapses every service onto one port, say. Only the profile named in the discovery
    /// settings is applied, and only one level deep: overrides of an override are ignored.
    /// </remarks>
    public IDictionary<string, ServiceDefinition>? Overrides { get; set; }
}
