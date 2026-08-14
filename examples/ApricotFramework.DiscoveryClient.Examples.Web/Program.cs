using ApricotFramework.DiscoveryClient;
using ApricotFramework.DiscoveryClient.AspNetCore.Extensions;
using ApricotFramework.DiscoveryClient.Examples.Web;
using ApricotFramework.DiscoveryClient.Model;

var builder = WebApplication.CreateBuilder(args);

// The catalogue. Sources accumulate, so a second one could restate a service for one environment
// without restating the rest — and the later registration would win.
builder.Services.AddServiceDefinitionsSource<CatalogueSource>();

// Binds the "StaticDiscovery" section and validates it, so a misspelled hosting type stops the host
// here rather than turning into null URLs somewhere later.
builder.Services.AddDiscoveryClient(builder.Configuration);

var app = builder.Build();

// The three named accessors. Every one answers null rather than throwing when the service is unknown
// or exposes no port for the role, which is why this reports them as JSON nulls instead of a 500.
app.MapGet("/resolve/{service}", async (string service, IDiscoveryClient discovery) => Results.Ok(new
{
    Service = service,
    ApiBase = await discovery.GetApiBase(service),
    GrpcUrl = await discovery.GetGrpcUrl(service),
    OrdersUrl = await discovery.GetApiUrl(service, "/v1/orders"),
}));

// The role-general form, and the only way to reach the 'ui' role — the catalogue declares ports for
// it, but no named accessor resolves it.
app.MapGet("/resolve/{service}/{role}", async (string service, string role, IDiscoveryClient discovery) =>
{
    var baseUrl = await discovery.GetBaseUrl(service, role);

    return baseUrl is null
        ? Results.NotFound(new { Service = service, Role = role, Error = "No port serves that role on the configured protocol." })
        : Results.Ok(new { Service = service, Role = role, BaseUrl = baseUrl });
});

// What the catalogue says, before any URL is built. Useful for seeing a profile applied: under the
// istio profile the api and grpc ports appear twice, and the second one is the one that wins.
app.MapGet("/definitions/{service}", async (string service, IServiceRegistry registry, IConfiguration configuration) =>
{
    var profile = configuration["StaticDiscovery:Profile"];

    var definition = await registry.GetDefinition(service, profile);

    return definition is null
        ? Results.NotFound(new { Service = service, Error = "Not in the catalogue." })
        : Results.Ok(new
        {
            Service = service,
            Profile = profile,
            definition.Namespace,
            definition.Domain,
            Ports = definition.Ports?.Select(port => new { port.Protocol, port.Roles, port.Port }),
        });
});

// The whole story in one response: the same catalogue resolved for each hosting environment, which is
// the point of the library — one catalogue, a laptop and a cluster.
app.MapGet("/demo", async (IServiceRegistry registry) =>
{
    const string Service = "svc-one";

    var hostingTypes = new[]
    {
        StaticHostingTypes.Localhost,
        StaticHostingTypes.Docker,
        StaticHostingTypes.DockerLocalhost,
        StaticHostingTypes.K8s,
    };

    var rows = new List<object>();

    foreach (var hostingType in hostingTypes)
    {
        foreach (var profile in new[] { null, "istio" })
        {
            // Built by hand rather than resolved from the container, to resolve the same catalogue
            // under settings the running host does not have. This is what the core package alone
            // gives a console app or a worker.
            var client = new ApricotFramework.DiscoveryClient.Impl.DefaultStaticDiscoveryClient(
                registry,
                new StaticDiscoveryOptions
                {
                    HostingType = hostingType,
                    Profile = profile,
                    DefaultProtocol = ServiceProtocolTypes.Https,
                    DefaultGrpcProtocol = ServiceProtocolTypes.Https,
                });

            rows.Add(new
            {
                HostingType = hostingType,
                Profile = profile ?? "(none)",
                Api = await client.GetApiBase(Service),

                // The istio profile moves api and grpc to 443 and says nothing about ui, so ui stays on
                // the service's own port. That is the override rule visible in one row.
                Ui = await client.GetBaseUrl(Service, ServicePortRoles.Ui),
                Grpc = await client.GetGrpcUrl(Service),
            });
        }
    }

    return Results.Ok(new { Service, Resolved = rows });
});

app.Run();
