namespace ApricotFramework.DiscoveryClient.Model;

/// <summary>
/// One port a service listens on, and what it serves there.
/// </summary>
public class ServicePortDefinition
{
    /// <summary>
    /// The protocol the port speaks — see <see cref="ServiceProtocolTypes"/>.
    /// </summary>
    /// <remarks>
    /// Matched case-insensitively against the configured default protocol, so a catalogue may write
    /// either <c>http</c> or <c>HTTP</c>.
    /// </remarks>
    public string? Protocol { get; set; }

    /// <summary>
    /// The roles the port serves — see <see cref="ServicePortRoles"/>.
    /// </summary>
    /// <remarks>
    /// Matched case-sensitively, unlike <see cref="Protocol"/>: a role is an arbitrary label a
    /// catalogue chooses, not a value from a fixed set, so there is no canonical casing to fold to.
    /// A port with no roles is never selected.
    /// </remarks>
    public IEnumerable<string>? Roles { get; set; }

    /// <summary>
    /// The port number.
    /// </summary>
    public int Port { get; set; }
}
