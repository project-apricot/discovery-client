using ApricotFramework.DiscoveryClient.Model;

namespace ApricotFramework.DiscoveryClient;

/// <summary>
/// Answers what a service's definition is.
/// </summary>
/// <remarks>
/// Where a discovery client turns a definition into a URL, a registry decides what the definition
/// is. Splitting them is what lets the definitions come from somewhere other than a static
/// catalogue — a cluster API or a registry service — without touching URL building. The methods are
/// awaitable for that reason, even though the implementation shipped here answers from memory.
/// </remarks>
public interface IServiceRegistry
{
    /// <summary>
    /// Gets a service's definition as written, with no profile applied.
    /// </summary>
    /// <param name="service">The service name.</param>
    /// <returns>The definition, or <see langword="null"/> if the service is unknown.</returns>
    ValueTask<IServiceDefinition?> GetDefinition(string service);

    /// <summary>
    /// Gets a service's definition with a profile's overrides applied.
    /// </summary>
    /// <param name="service">The service name.</param>
    /// <param name="profile">
    /// The profile to apply, or <see langword="null"/> for the definition as written.
    /// </param>
    /// <returns>The definition, or <see langword="null"/> if the service is unknown.</returns>
    /// <remarks>
    /// A profile that the service declares no overrides for is not an error: the definition is
    /// returned as written.
    /// </remarks>
    ValueTask<IServiceDefinition?> GetDefinition(string service, string? profile);
}
