using ApricotFramework.DiscoveryClient.AspNetCore.Validation;
using ApricotFramework.DiscoveryClient.Model;

namespace ApricotFramework.DiscoveryClient.AspNetCore.Tests;

/// <summary>
/// Covers which settings are rejected at startup, and — as much to the point — which are not.
/// </summary>
public class StaticDiscoveryOptionsValidatorTests
{
    [Fact]
    public void Validate_NothingConfigured_Succeeds()
    {
        // Every setting is optional, so an unconfigured host must start.
        Assert.True(Validate(new StaticDiscoveryOptions()).Succeeded);
    }

    [Theory]
    [InlineData(StaticHostingTypes.Localhost)]
    [InlineData(StaticHostingTypes.Docker)]
    [InlineData(StaticHostingTypes.DockerLocalhost)]
    [InlineData(StaticHostingTypes.K8s)]
    public void Validate_KnownHostingType_Succeeds(string hostingType)
    {
        Assert.True(Validate(new StaticDiscoveryOptions { HostingType = hostingType }).Succeeded);
    }

    [Theory]
    [InlineData("kubernetes")]
    [InlineData("k8")]
    [InlineData("K8S")]
    [InlineData("docker-localhost")]
    [InlineData("mainframe")]
    public void Validate_UnrecognisedHostingType_Fails(string hostingType)
    {
        var result = Validate(new StaticDiscoveryOptions { HostingType = hostingType });

        Assert.True(result.Failed);
        Assert.Contains(hostingType, result.FailureMessage, StringComparison.Ordinal);

        // The message has to name the accepted values, since the whole point is that the operator
        // wrote something close to one of them.
        Assert.Contains(StaticHostingTypes.DockerLocalhost, result.FailureMessage, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_BlankHostingType_Succeeds(string hostingType)
    {
        Assert.True(Validate(new StaticDiscoveryOptions { HostingType = hostingType }).Succeeded);
    }

    [Fact]
    public void Validate_HostingTypeWithSurroundingWhitespace_Succeeds()
    {
        Assert.True(Validate(new StaticDiscoveryOptions { HostingType = "  k8s  " }).Succeeded);
    }

    [Theory]
    [InlineData("http")]
    [InlineData("https")]
    [InlineData("HTTPS")]
    [InlineData("Http")]
    [InlineData("  https  ")]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_UsableProtocol_Succeeds(string? protocol)
    {
        Assert.True(Validate(new StaticDiscoveryOptions { DefaultProtocol = protocol }).Succeeded);
        Assert.True(Validate(new StaticDiscoveryOptions { DefaultGrpcProtocol = protocol }).Succeeded);
    }

    [Theory]
    [InlineData("grpc")]
    [InlineData("h2")]
    [InlineData("tcp")]
    [InlineData("https://")]
    public void Validate_ProtocolThatCannotAppearInAUrl_Fails(string protocol)
    {
        var byDefault = Validate(new StaticDiscoveryOptions { DefaultProtocol = protocol });

        Assert.True(byDefault.Failed);
        Assert.Contains(nameof(StaticDiscoveryOptions.DefaultProtocol), byDefault.FailureMessage, StringComparison.Ordinal);

        var forGrpc = Validate(new StaticDiscoveryOptions { DefaultGrpcProtocol = protocol });

        Assert.True(forGrpc.Failed);
        Assert.Contains(nameof(StaticDiscoveryOptions.DefaultGrpcProtocol), forGrpc.FailureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_SeveralMistakesAtOnce_ReportsThemAll()
    {
        // An operator fixing a configuration file should see every problem in it, not the first one.
        var result = Validate(new StaticDiscoveryOptions
        {
            HostingType = "kubernetes",
            DefaultProtocol = "tcp",
            DefaultGrpcProtocol = "h2",
        });

        Assert.True(result.Failed);
        Assert.Equal(3, result.Failures!.Count());
    }

    [Fact]
    public void Validate_NullOptions_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Validate(null!));
    }

    /// <summary>
    /// Validates one set of settings.
    /// </summary>
    /// <param name="options">The settings to validate.</param>
    /// <returns>The result.</returns>
    private static Microsoft.Extensions.Options.ValidateOptionsResult Validate(StaticDiscoveryOptions options)
    {
        return new StaticDiscoveryOptionsValidator().Validate(name: null, options);
    }
}
