using ApricotFramework.DiscoveryClient.Model;

namespace ApricotFramework.DiscoveryClient;

/// <summary>
/// Supplies a catalogue of service definitions.
/// </summary>
/// <remarks>
/// Implement this to bring definitions in from wherever they live — an embedded file, a mounted
/// volume, configuration, a literal in code. Several sources can be registered at once and are
/// merged by service name, with later sources winning, so a deployment-specific source can restate
/// one service without restating the catalogue.
/// </remarks>
public interface IServiceDefinitionsSource
{
    /// <summary>
    /// Gets this source's catalogue.
    /// </summary>
    /// <returns>
    /// The catalogue. Returning <see langword="null"/>, or a catalogue with no services, contributes
    /// nothing and is not an error.
    /// </returns>
    /// <remarks>
    /// Called once while the registry is being built, so an expensive read here is paid once per
    /// host rather than per lookup.
    /// </remarks>
    ServiceDefinitions? GetDefinitions();
}
