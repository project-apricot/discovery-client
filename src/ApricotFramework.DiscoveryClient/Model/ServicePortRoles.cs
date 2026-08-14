namespace ApricotFramework.DiscoveryClient.Model;

/// <summary>
/// The roles a port can carry in a service definition.
/// </summary>
/// <remarks>
/// A role says what kind of traffic a port serves, so one service can expose several ports and a
/// caller can ask for the one it needs. The values are the strings written under <c>roles:</c> in a
/// service catalogue, so they are a stored contract and do not change. A catalogue may use roles
/// beyond these; they are matched by string, not by this list.
/// </remarks>
public static class ServicePortRoles
{
    /// <summary>
    /// The port serving the service's HTTP API.
    /// </summary>
    public const string Api = "api";

    /// <summary>
    /// The port serving the service's user interface.
    /// </summary>
    public const string Ui = "ui";

    /// <summary>
    /// The port serving the service's gRPC endpoints.
    /// </summary>
    public const string Grpc = "grpc";
}
