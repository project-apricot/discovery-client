# ApricotFramework.DiscoveryClient

[![NuGet](https://img.shields.io/nuget/v/ApricotFramework.DiscoveryClient.svg?label=ApricotFramework.DiscoveryClient)](https://www.nuget.org/packages/ApricotFramework.DiscoveryClient/)
[![NuGet](https://img.shields.io/nuget/v/ApricotFramework.DiscoveryClient.AspNetCore.svg?label=ApricotFramework.DiscoveryClient.AspNetCore)](https://www.nuget.org/packages/ApricotFramework.DiscoveryClient.AspNetCore/)
[![NuGet](https://img.shields.io/nuget/v/ApricotFramework.DiscoveryClient.Yaml.svg?label=ApricotFramework.DiscoveryClient.Yaml)](https://www.nuget.org/packages/ApricotFramework.DiscoveryClient.Yaml/)
[![CI](https://github.com/project-apricot/discovery-client/actions/workflows/ci.yml/badge.svg)](https://github.com/project-apricot/discovery-client/actions/workflows/ci.yml)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](https://github.com/project-apricot/discovery-client/blob/main/LICENSE)

Service discovery for .NET without a discovery server: one declarative catalogue of services and
ports, resolved to URLs differently on a laptop, in containers and in Kubernetes.

`ApricotFramework.DiscoveryClient` is the **zero-dependency** core.

## Install

```bash
dotnet add package ApricotFramework.DiscoveryClient
dotnet add package ApricotFramework.DiscoveryClient.AspNetCore
dotnet add package ApricotFramework.DiscoveryClient.Yaml
```

## Usage

```yaml
# services.yaml — the catalogue, shared by every service that talks to these
services:
  billing:
    namespace: payments
    ports:
      - protocol: https
        roles: [ 'api' ]
        port: 11101
      - protocol: https
        roles: [ 'ui' ]
        port: 11103
      - protocol: https
        roles: [ 'grpc' ]
        port: 11105
    overrides:
      istio:                      # behind an ingress everything arrives on 443
        ports:
          - protocol: https
            roles: [ 'api', 'grpc' ]
            port: 443
```

```jsonc
// appsettings.json — the only thing that differs between environments
{
  "StaticDiscovery": {
    "HostingType": "k8s",         // localhost | docker | docker_localhost | k8s
    "Profile": "istio",
    "DefaultProtocol": "https",
    "DefaultGrpcProtocol": "https"
  }
}
```

```csharp
using ApricotFramework.DiscoveryClient.AspNetCore.Extensions;

builder.Services.AddServiceDefinitionsSource<CatalogueSource>();
builder.Services.AddDiscoveryClient(builder.Configuration);
```

```csharp
// https://billing.payments.svc.cluster.local:443
await discovery.GetApiBase("billing");

// https://billing.payments.svc.cluster.local:443/v1/invoices
await discovery.GetApiUrl("billing", "/v1/invoices");

// The same catalogue with HostingType "localhost" gives https://localhost:11101 — no code changes,
// and an unknown service is a null rather than an exception.
```

A profile's ports are **appended** to the service's own, and the last port matching a protocol and
role wins. That is what makes an override take effect, and it means an override that says nothing
about a role leaves that role on the service's own port — `ui` above is still reachable on 11103 under
the `istio` profile.

Full documentation: <https://projectapricot.dev/docs/discovery-client>
