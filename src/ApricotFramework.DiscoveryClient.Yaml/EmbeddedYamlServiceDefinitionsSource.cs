using System.Reflection;

namespace ApricotFramework.DiscoveryClient.Yaml;

/// <summary>
/// A source that reads a service catalogue from a YAML file embedded in an assembly.
/// </summary>
/// <remarks>
/// Embedding the catalogue ships it with the code that reads it, so a deployment cannot start with
/// the file missing or a stale copy of it. The trade is that changing the catalogue means a rebuild.
/// </remarks>
public class EmbeddedYamlServiceDefinitionsSource : YamlServiceDefinitionsSource
{
    /// <summary>
    /// Creates a new source over an embedded resource.
    /// </summary>
    /// <param name="resource">
    /// The resource path relative to the root namespace, with directory separators written as dots —
    /// <c>EmbeddedResources.services.yaml</c> for <c>EmbeddedResources/services.yaml</c>.
    /// </param>
    /// <param name="assembly">The assembly holding the resource.</param>
    /// <param name="rootNamespace">
    /// The prefix the resource name was built with, or <see langword="null"/> to use the assembly
    /// name. Needed only when the project sets a root namespace that differs from its assembly name.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="assembly"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the assembly holds no such resource. Raised while constructing the source rather
    /// than when the catalogue is first read, so the mistake surfaces at startup.
    /// </exception>
    public EmbeddedYamlServiceDefinitionsSource(string resource, Assembly assembly, string? rootNamespace = null)
        : base(OpenEmbeddedResource(resource, assembly, rootNamespace))
    {
    }

    /// <summary>
    /// Resolves an embedded resource to a supplier that opens it.
    /// </summary>
    /// <param name="resource">The resource path relative to the root namespace.</param>
    /// <param name="assembly">The assembly holding the resource.</param>
    /// <param name="rootNamespace">The prefix the resource name was built with, if not the assembly name.</param>
    /// <returns>A supplier that opens the resource.</returns>
    private static Func<Stream?> OpenEmbeddedResource(string resource, Assembly assembly, string? rootNamespace)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var prefix = string.IsNullOrWhiteSpace(rootNamespace) ? assembly.GetName().Name : rootNamespace;

        var path = string.IsNullOrWhiteSpace(prefix) ? resource : $"{prefix}.{resource}";

        // Checked now rather than on first read. A missing resource is a build mistake — the file was
        // not marked as an embedded resource, or the path spells the folder wrong — and the message
        // is only useful next to the name that was looked for.
        if (!assembly.GetManifestResourceNames().Contains(path, StringComparer.Ordinal))
        {
            throw new ArgumentException(
                $"The assembly '{assembly.GetName().Name}' holds no embedded resource '{path}'. Check that the file is included as an EmbeddedResource and that the path is written with dots for folder separators.",
                nameof(resource));
        }

        return () => assembly.GetManifestResourceStream(path);
    }
}
