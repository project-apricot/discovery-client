using System.Reflection;

namespace ApricotFramework.DiscoveryClient.Yaml.Tests;

/// <summary>
/// Covers how an embedded catalogue is located, and what a consumer sees when it is not there.
/// </summary>
public class EmbeddedYamlServiceDefinitionsSourceTests
{
    /// <summary>
    /// The assembly holding the reference catalogue.
    /// </summary>
    private static Assembly TestAssembly => typeof(EmbeddedYamlServiceDefinitionsSourceTests).Assembly;

    [Fact]
    public void GetDefinitions_ResourceUnderTheAssemblyName_IsFound()
    {
        var source = new EmbeddedYamlServiceDefinitionsSource("Catalogues.services.yaml", TestAssembly);

        Assert.NotNull(source.GetDefinitions()?.Services);
    }

    [Fact]
    public void GetDefinitions_ExplicitRootNamespace_IsFound()
    {
        var source = new EmbeddedYamlServiceDefinitionsSource(
            "Catalogues.services.yaml",
            TestAssembly,
            TestAssembly.GetName().Name);

        Assert.NotNull(source.GetDefinitions()?.Services);
    }

    [Fact]
    public void Constructor_MissingResource_ThrowsNamingWhatWasLookedFor()
    {
        // The message has to carry the full resource name: the mistake is almost always the path
        // spelling or a missing EmbeddedResource entry, and neither is visible from the exception type.
        var exception = Assert.Throws<ArgumentException>(
            () => new EmbeddedYamlServiceDefinitionsSource("Catalogues.absent.yaml", TestAssembly));

        Assert.Contains("Catalogues.absent.yaml", exception.Message, StringComparison.Ordinal);
        Assert.Contains(TestAssembly.GetName().Name!, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_ResourcePathWrittenWithSlashes_Throws()
    {
        // Embedded resource names use dots for folder separators, so a path is not interchangeable.
        Assert.Throws<ArgumentException>(
            () => new EmbeddedYamlServiceDefinitionsSource("Catalogues/services.yaml", TestAssembly));
    }

    [Fact]
    public void Constructor_ResourceNameCasing_IsMatchedExactly()
    {
        Assert.Throws<ArgumentException>(
            () => new EmbeddedYamlServiceDefinitionsSource("catalogues.services.yaml", TestAssembly));
    }

    [Fact]
    public void Constructor_WrongRootNamespace_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => new EmbeddedYamlServiceDefinitionsSource("Catalogues.services.yaml", TestAssembly, "Not.The.Namespace"));
    }

    [Fact]
    public void Constructor_NullAssembly_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => new EmbeddedYamlServiceDefinitionsSource("Catalogues.services.yaml", null!));
    }
}
