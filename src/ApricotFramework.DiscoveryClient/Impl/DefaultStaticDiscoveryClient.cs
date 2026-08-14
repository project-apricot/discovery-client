namespace ApricotFramework.DiscoveryClient.Impl;

/// <summary>
/// A discovery client configured once, at construction.
/// </summary>
/// <remarks>
/// The client to use without a dependency injection container — a console tool, a worker, a test.
/// Its settings never change; for settings that follow configuration reloads, use the client from
/// the ASP.NET Core package.
/// </remarks>
public class DefaultStaticDiscoveryClient : BaseStaticDiscoveryClient
{
    /// <summary>
    /// The settings this client was created with.
    /// </summary>
    private readonly StaticDiscoveryOptions options;

    /// <summary>
    /// Creates a new client over a registry and a fixed set of settings.
    /// </summary>
    /// <param name="serviceRegistry">The registry supplying definitions.</param>
    /// <param name="options">The settings to resolve with.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="serviceRegistry"/> or <paramref name="options"/> is null.
    /// </exception>
    public DefaultStaticDiscoveryClient(IServiceRegistry serviceRegistry, StaticDiscoveryOptions options)
        : base(serviceRegistry)
    {
        ArgumentNullException.ThrowIfNull(options);

        this.options = options;
    }

    /// <inheritdoc />
    protected override StaticDiscoveryOptions GetCurrentOptions()
    {
        return this.options;
    }
}
