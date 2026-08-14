namespace ApricotFramework.DiscoveryClient.Model;

/// <summary>
/// A service definition with a profile's overrides already applied.
/// </summary>
/// <remarks>
/// What a registry returns once it has merged a profile in, which is why it carries no
/// <see cref="ServiceDefinition.Overrides"/> of its own — there is nothing left to apply.
/// </remarks>
public class EffectiveServiceDefinition : IServiceDefinition
{
    /// <inheritdoc cref="IServiceDefinition.Namespace" />
    public string? Namespace { get; set; }

    /// <inheritdoc cref="IServiceDefinition.Domain" />
    public string? Domain { get; set; }

    /// <inheritdoc cref="IServiceDefinition.Ports" />
    public IEnumerable<ServicePortDefinition>? Ports { get; set; }
}
