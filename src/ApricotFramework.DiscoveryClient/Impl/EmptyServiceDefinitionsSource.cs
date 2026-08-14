using ApricotFramework.DiscoveryClient.Model;

namespace ApricotFramework.DiscoveryClient.Impl;

/// <summary>
/// A source that declares no services.
/// </summary>
/// <remarks>
/// Registered so a registry always has at least one source and a host with no catalogue starts
/// rather than failing to resolve <see cref="IServiceRegistry"/>. Every lookup then answers null,
/// which is the honest result for a catalogue nobody supplied.
/// </remarks>
public class EmptyServiceDefinitionsSource : StaticServiceDefinitionsSource
{
    /// <summary>
    /// Creates a new empty source.
    /// </summary>
    public EmptyServiceDefinitionsSource()
        : base(new ServiceDefinitions { Services = new Dictionary<string, ServiceDefinition>(StringComparer.Ordinal) })
    {
    }
}
