namespace ApricotFramework.DiscoveryClient.Model;

/// <summary>
/// The protocols a port can be reached over.
/// </summary>
/// <remarks>
/// The values are the strings written under <c>protocol:</c> in a service catalogue and configured
/// as the default protocol, so they are a stored contract and do not change. These two are the only
/// ones a resolved URL can use: anything else has no scheme to build a URL from.
/// </remarks>
public static class ServiceProtocolTypes
{
    /// <summary>
    /// Plain HTTP.
    /// </summary>
    public const string Http = "http";

    /// <summary>
    /// HTTP over TLS.
    /// </summary>
    public const string Https = "https";
}
