using ApricotFramework.DiscoveryClient.Model;
using Microsoft.Extensions.Options;

namespace ApricotFramework.DiscoveryClient.AspNetCore.Validation;

/// <summary>
/// Rejects discovery settings that could only ever resolve to nothing.
/// </summary>
/// <remarks>
/// Both settings this checks fail silently otherwise: a misspelled hosting type sends every call to
/// a plausible-looking wrong host, and a protocol that is not <c>http</c> or <c>https</c> makes every
/// lookup answer null. Neither raises anything at the point of failure, so a service starts and only
/// looks broken later, from somewhere else. Failing at startup puts the message where the mistake is.
/// <para>
/// An unset value is not a mistake, so it validates: a host with no discovery section keeps starting
/// and resolves services to their own name over HTTP.
/// </para>
/// </remarks>
public sealed class StaticDiscoveryOptionsValidator : IValidateOptions<StaticDiscoveryOptions>
{
    /// <summary>
    /// The hosting types a resolved hostname can be built for.
    /// </summary>
    private static readonly string[] KnownHostingTypes =
    [
        StaticHostingTypes.Localhost,
        StaticHostingTypes.Docker,
        StaticHostingTypes.DockerLocalhost,
        StaticHostingTypes.K8s
    ];

    /// <summary>
    /// The protocols a resolved URL can use.
    /// </summary>
    private static readonly string[] SupportedProtocols =
    [
        ServiceProtocolTypes.Http,
        ServiceProtocolTypes.Https
    ];

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, StaticDiscoveryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        var hostingType = options.HostingType?.Trim();

        // Compared ordinally, and case-sensitively, because these are protocol tokens rather than
        // words: the catalogue and the configuration have to agree on one spelling, and folding case
        // here would accept 'K8S' while the resolver, matching the constant, would not.
        if (!string.IsNullOrEmpty(hostingType) && !KnownHostingTypes.Contains(hostingType, StringComparer.Ordinal))
        {
            failures.Add(
                $"{nameof(StaticDiscoveryOptions.HostingType)} '{hostingType}' is not recognised. Use one of {string.Join(", ", KnownHostingTypes)}, or leave it unset to reach every service by its own name.");
        }

        AddProtocolFailure(failures, nameof(StaticDiscoveryOptions.DefaultProtocol), options.DefaultProtocol);
        AddProtocolFailure(failures, nameof(StaticDiscoveryOptions.DefaultGrpcProtocol), options.DefaultGrpcProtocol);

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    /// <summary>
    /// Records a failure when a protocol setting cannot appear in a URL.
    /// </summary>
    /// <param name="failures">The failures collected so far.</param>
    /// <param name="settingName">The setting being checked, for the message.</param>
    /// <param name="configured">The configured value.</param>
    private static void AddProtocolFailure(List<string> failures, string settingName, string? configured)
    {
        var protocol = configured?.Trim();

        // Case is folded here, unlike the hosting type, because the resolver folds it too — a
        // protocol is matched case-insensitively against the catalogue.
        if (string.IsNullOrEmpty(protocol) || SupportedProtocols.Contains(protocol, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        failures.Add(
            $"{settingName} '{protocol}' cannot be used in a URL. Use {string.Join(" or ", SupportedProtocols)}, or leave it unset to default to {ServiceProtocolTypes.Http}.");
    }
}
