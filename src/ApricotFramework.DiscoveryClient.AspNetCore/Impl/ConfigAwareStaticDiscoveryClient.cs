using ApricotFramework.DiscoveryClient.Impl;
using Microsoft.Extensions.Options;

namespace ApricotFramework.DiscoveryClient.AspNetCore.Impl;

/// <summary>
/// A discovery client that reads its settings from the options system on every call, so a
/// configuration reload takes effect without restarting the host.
/// </summary>
/// <remarks>
/// Reloading matters more here than it looks: switching profile or protocol is how a deployment
/// changes where its calls go, and doing it without a restart is the difference between a
/// configuration change and a rollout.
/// </remarks>
public class ConfigAwareStaticDiscoveryClient : BaseStaticDiscoveryClient
{
    /// <summary>
    /// The monitor supplying the current settings.
    /// </summary>
    private readonly IOptionsMonitor<StaticDiscoveryOptions> optionsMonitor;

    /// <summary>
    /// Creates a new client over a registry and the options system.
    /// </summary>
    /// <param name="serviceRegistry">The registry supplying definitions.</param>
    /// <param name="optionsMonitor">The monitor supplying the current settings.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="serviceRegistry"/> or <paramref name="optionsMonitor"/> is null.
    /// </exception>
    public ConfigAwareStaticDiscoveryClient(IServiceRegistry serviceRegistry, IOptionsMonitor<StaticDiscoveryOptions> optionsMonitor)
        : base(serviceRegistry)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);

        this.optionsMonitor = optionsMonitor;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Read once per call rather than per setting, so a reload landing mid-resolution cannot produce
    /// a URL that mixes the old profile with the new protocol.
    /// </remarks>
    protected override StaticDiscoveryOptions GetCurrentOptions()
    {
        return this.optionsMonitor.CurrentValue;
    }
}
