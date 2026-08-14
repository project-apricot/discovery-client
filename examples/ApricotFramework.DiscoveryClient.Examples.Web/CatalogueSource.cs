using System.Reflection;
using ApricotFramework.DiscoveryClient.Yaml;

namespace ApricotFramework.DiscoveryClient.Examples.Web;

/// <summary>
/// The service catalogue, embedded in this assembly.
/// </summary>
/// <remarks>
/// Wrapping the embedded source in a named type is what lets it be registered by type rather than as
/// an instance, so the resource path and the assembly are stated once, next to the file they refer to.
/// </remarks>
public class CatalogueSource : EmbeddedYamlServiceDefinitionsSource
{
    /// <summary>
    /// Creates a new source over the embedded catalogue.
    /// </summary>
    public CatalogueSource()
        : base("EmbeddedResources.services.yaml", Assembly.GetExecutingAssembly())
    {
    }
}
