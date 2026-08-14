using ApricotFramework.DiscoveryClient.Model;

namespace ApricotFramework.DiscoveryClient.Impl;

/// <summary>
/// A source over a catalogue already in hand.
/// </summary>
/// <remarks>
/// Useful for definitions built in code, and the base for sources that read a catalogue eagerly.
/// </remarks>
public class StaticServiceDefinitionsSource : IServiceDefinitionsSource
{
    /// <summary>
    /// The catalogue this source supplies.
    /// </summary>
    private readonly ServiceDefinitions definitions;

    /// <summary>
    /// Creates a new source over a catalogue.
    /// </summary>
    /// <param name="definitions">The catalogue to supply.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="definitions"/> is null.
    /// </exception>
    public StaticServiceDefinitionsSource(ServiceDefinitions definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        this.definitions = definitions;
    }

    /// <inheritdoc />
    public ServiceDefinitions GetDefinitions()
    {
        return this.definitions;
    }
}
