using ApricotFramework.DiscoveryClient.AspNetCore.Impl;
using ApricotFramework.DiscoveryClient.AspNetCore.Validation;
using ApricotFramework.DiscoveryClient.Impl;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace ApricotFramework.DiscoveryClient.AspNetCore.Extensions;

/// <summary>
/// Registers service discovery in a service collection.
/// </summary>
public static class DiscoveryClientServiceCollectionExtensions
{
    /// <summary>
    /// The configuration section bound when no other is named.
    /// </summary>
    public const string ConfigurationSectionName = "StaticDiscovery";

    /// <summary>
    /// Adds service discovery, binding its settings from the
    /// <see cref="ConfigurationSectionName"/> configuration section.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration to bind from.</param>
    /// <returns>The service collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="configuration"/> is null.
    /// </exception>
    public static IServiceCollection AddDiscoveryClient(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddDiscoveryClient(configuration, ConfigurationSectionName);
    }

    /// <summary>
    /// Adds service discovery, binding its settings from a named configuration section.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration to bind from.</param>
    /// <param name="sectionName">The section to bind.</param>
    /// <returns>The service collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/>, <paramref name="configuration"/> or
    /// <paramref name="sectionName"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="sectionName"/> is empty or white space.
    /// </exception>
    public static IServiceCollection AddDiscoveryClient(this IServiceCollection services, IConfiguration configuration, string sectionName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        services.AddOptions<StaticDiscoveryOptions>().Bind(configuration.GetSection(sectionName));

        return AddDiscoveryClientCore(services);
    }

    /// <summary>
    /// Adds service discovery, configuring its settings in code.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configures the settings.</param>
    /// <returns>The service collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="configure"/> is null.
    /// </exception>
    public static IServiceCollection AddDiscoveryClient(this IServiceCollection services, Action<StaticDiscoveryOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<StaticDiscoveryOptions>().Configure(configure);

        return AddDiscoveryClientCore(services);
    }

    /// <summary>
    /// Replaces the discovery client with an implementation of your own.
    /// </summary>
    /// <typeparam name="TClient">The client to register as a singleton.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    /// <remarks>
    /// Order does not matter. Called before <c>AddDiscoveryClient</c>, this registration is the one
    /// that stands because the built-in client is only added if none is present; called after, it
    /// stands because the last registration of a service is the one resolved.
    /// </remarks>
    public static IServiceCollection AddDiscoveryClient<TClient>(this IServiceCollection services)
        where TClient : class, IDiscoveryClient
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IDiscoveryClient, TClient>();

        return services;
    }

    /// <summary>
    /// Replaces the discovery client with an instance of your own.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="client">The client to register.</param>
    /// <returns>The service collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="client"/> is null.
    /// </exception>
    public static IServiceCollection AddDiscoveryClient(this IServiceCollection services, IDiscoveryClient client)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(client);

        services.AddSingleton(client);

        return services;
    }

    /// <summary>
    /// Adds a source of service definitions.
    /// </summary>
    /// <typeparam name="TSource">The source to register as a singleton.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    /// <remarks>
    /// Sources accumulate rather than replace one another, and are merged by service name in
    /// registration order — so where two sources declare the same service, the one registered later
    /// wins. Registering the same source type twice registers it twice.
    /// </remarks>
    public static IServiceCollection AddServiceDefinitionsSource<TSource>(this IServiceCollection services)
        where TSource : class, IServiceDefinitionsSource
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IServiceDefinitionsSource, TSource>();

        return services;
    }

    /// <summary>
    /// Adds a source of service definitions.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="source">The source to register.</param>
    /// <returns>The service collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="source"/> is null.
    /// </exception>
    public static IServiceCollection AddServiceDefinitionsSource(this IServiceCollection services, IServiceDefinitionsSource source)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(source);

        services.AddSingleton(source);

        return services;
    }

    /// <summary>
    /// Replaces the service registry with an implementation of your own.
    /// </summary>
    /// <typeparam name="TRegistry">The registry to register as a singleton.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    /// <remarks>
    /// Replacing the registry replaces where definitions come from, so registered
    /// <see cref="IServiceDefinitionsSource"/> instances are only consulted if your registry consults
    /// them.
    /// </remarks>
    public static IServiceCollection AddServiceRegistry<TRegistry>(this IServiceCollection services)
        where TRegistry : class, IServiceRegistry
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IServiceRegistry, TRegistry>();

        return services;
    }

    /// <summary>
    /// Replaces the service registry with an instance of your own.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="registry">The registry to register.</param>
    /// <returns>The service collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="registry"/> is null.
    /// </exception>
    public static IServiceCollection AddServiceRegistry(this IServiceCollection services, IServiceRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(registry);

        services.AddSingleton(registry);

        return services;
    }

    /// <summary>
    /// Registers the validator, the registry, a fallback source and the client itself.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    private static IServiceCollection AddDiscoveryClientCore(IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<StaticDiscoveryOptions>, StaticDiscoveryOptionsValidator>());

        // Surface a misspelled hosting type or an unusable protocol when the host starts rather than
        // as a null URL somewhere later. In a bare ServiceProvider there is nothing to run the
        // startup hook, so the validator still runs on first access to the options.
        services.AddOptions<StaticDiscoveryOptions>().ValidateOnStart();

        // A registry needs at least one source to resolve at all, so this guarantees one. It declares
        // nothing, and sources are merged, so it cannot mask a real catalogue — and TryAddEnumerable
        // keeps a second AddDiscoveryClient call from registering it twice.
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IServiceDefinitionsSource, EmptyServiceDefinitionsSource>());

        services.TryAddSingleton<IServiceRegistry, DefaultServiceRegistry>();
        services.TryAddSingleton<IDiscoveryClient, ConfigAwareStaticDiscoveryClient>();

        return services;
    }
}
