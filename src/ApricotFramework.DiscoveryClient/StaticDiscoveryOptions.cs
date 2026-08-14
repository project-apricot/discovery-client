namespace ApricotFramework.DiscoveryClient;

/// <summary>
/// The settings that decide how a catalogue is turned into URLs.
/// </summary>
/// <remarks>
/// Every setting is optional, and leaving all of them unset is a usable configuration: services
/// resolve to their own name over HTTP, which is what a container network wants.
/// </remarks>
public class StaticDiscoveryOptions
{
    /// <summary>
    /// The hosting environment to resolve hostnames for — see
    /// <see cref="Model.StaticHostingTypes"/>.
    /// </summary>
    /// <remarks>
    /// An unset or unrecognised value resolves a service to its own name. That is deliberate: it is
    /// the right answer on any network where service names are resolvable, so a bespoke value costs
    /// nothing when the client is built by hand. Registering through the ASP.NET Core package
    /// rejects an unrecognised value instead, on the grounds that in configuration it is far more
    /// likely a typo.
    /// </remarks>
    public string? HostingType { get; set; }

    /// <summary>
    /// The profile whose overrides to apply, or <see langword="null"/> for definitions as written.
    /// </summary>
    public string? Profile { get; set; }

    /// <summary>
    /// The protocol to resolve every role except gRPC over. Defaults to
    /// <see cref="Model.ServiceProtocolTypes.Http"/>.
    /// </summary>
    /// <remarks>
    /// Only <c>http</c> and <c>https</c> can produce a URL; any other value resolves to
    /// <see langword="null"/> for every service.
    /// </remarks>
    public string? DefaultProtocol { get; set; }

    /// <summary>
    /// The protocol to resolve the gRPC role over. Defaults to
    /// <see cref="Model.ServiceProtocolTypes.Http"/>.
    /// </summary>
    /// <remarks>
    /// Separate from <see cref="DefaultProtocol"/> because gRPC frequently differs on TLS: an
    /// ingress that terminates TLS for the HTTP API often passes gRPC through untouched, or the
    /// reverse.
    /// </remarks>
    public string? DefaultGrpcProtocol { get; set; }
}
