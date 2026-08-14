using ApricotFramework.DiscoveryClient.Model;

namespace ApricotFramework.DiscoveryClient.Impl;

/// <summary>
/// A registry over a catalogue merged from every registered source.
/// </summary>
/// <remarks>
/// The sources are read once, when the registry is constructed, so lookups are dictionary reads and
/// a change to a catalogue file takes effect on the next start. Register this as a singleton.
/// </remarks>
public class DefaultServiceRegistry : IServiceRegistry
{
    /// <summary>
    /// The merged catalogue, keyed by service name.
    /// </summary>
    private readonly Dictionary<string, ServiceDefinition> definitions;

    /// <summary>
    /// Creates a new registry from a set of sources.
    /// </summary>
    /// <param name="sources">
    /// The sources to merge, in order. Where two declare the same service, the later one wins
    /// outright — its definition replaces rather than merges with the earlier one.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="sources"/> is null.</exception>
    public DefaultServiceRegistry(IEnumerable<IServiceDefinitionsSource> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);

        this.definitions = Merge(sources);
    }

    /// <inheritdoc />
    public ValueTask<IServiceDefinition?> GetDefinition(string service)
    {
        return new ValueTask<IServiceDefinition?>(this.Find(service));
    }

    /// <inheritdoc />
    public ValueTask<IServiceDefinition?> GetDefinition(string service, string? profile)
    {
        var definition = this.Find(service);

        if (definition is null)
        {
            return new ValueTask<IServiceDefinition?>(result: null);
        }

        if (string.IsNullOrWhiteSpace(profile) || definition.Overrides is null)
        {
            return new ValueTask<IServiceDefinition?>(definition);
        }

        if (!definition.Overrides.TryGetValue(profile, out var overrides))
        {
            return new ValueTask<IServiceDefinition?>(definition);
        }

        return new ValueTask<IServiceDefinition?>(Apply(definition, overrides));
    }

    /// <summary>
    /// Applies a profile's overrides to a definition.
    /// </summary>
    /// <param name="definition">The definition as written.</param>
    /// <param name="overrides">The profile's overrides.</param>
    /// <returns>The effective definition.</returns>
    /// <remarks>
    /// A named value in the override replaces the one on the definition; an absent one leaves it
    /// alone. Ports are the exception: they are appended rather than replaced, so an override that
    /// restates the api port leaves the ports it does not mention still reachable. Appending is what
    /// makes it win, because a lookup takes the last port matching its protocol and role.
    /// </remarks>
    private static EffectiveServiceDefinition Apply(ServiceDefinition definition, ServiceDefinition overrides)
    {
        return new EffectiveServiceDefinition
        {
            Namespace = string.IsNullOrWhiteSpace(overrides.Namespace) ? definition.Namespace : overrides.Namespace,
            Domain = string.IsNullOrWhiteSpace(overrides.Domain) ? definition.Domain : overrides.Domain,
            Ports = (definition.Ports ?? []).Concat(overrides.Ports ?? []),
        };
    }

    /// <summary>
    /// Merges every source's catalogue into one.
    /// </summary>
    /// <param name="sources">The sources to merge, in order.</param>
    /// <returns>The merged catalogue.</returns>
    private static Dictionary<string, ServiceDefinition> Merge(IEnumerable<IServiceDefinitionsSource> sources)
    {
        var merged = new Dictionary<string, ServiceDefinition>(StringComparer.Ordinal);

        foreach (var source in sources)
        {
            // A source with nothing to say may return either a null catalogue or a null service map,
            // and neither is an error — an empty catalogue file produces the first.
            var services = source.GetDefinitions()?.Services;

            if (services is null)
            {
                continue;
            }

            foreach (var (name, definition) in services)
            {
                // A catalogue may name a service and say nothing about it — `svc-one:` with no body
                // parses to a null definition. Dropping it keeps every value in the merged catalogue
                // non-null, so nothing downstream has to ask, and it means an empty entry in a later
                // source adds nothing rather than erasing what an earlier one said. Reading it as an
                // erasure would be the more surprising of the two, and it is not a thing anyone
                // writes on purpose.
                if (definition is null)
                {
                    continue;
                }

                merged[name] = definition;
            }
        }

        return merged;
    }

    /// <summary>
    /// Finds a service's definition as written.
    /// </summary>
    /// <param name="service">The service name.</param>
    /// <returns>The definition, or <see langword="null"/> if the service is unknown.</returns>
    private ServiceDefinition? Find(string service)
    {
        // A blank name cannot identify a service, and answering null keeps a caller that built the
        // name from an empty setting on the same path as one that misspelled it.
        if (string.IsNullOrWhiteSpace(service))
        {
            return null;
        }

        return this.definitions.GetValueOrDefault(service);
    }
}
