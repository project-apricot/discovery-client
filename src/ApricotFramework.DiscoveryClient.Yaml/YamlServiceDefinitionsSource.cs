using ApricotFramework.DiscoveryClient.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ApricotFramework.DiscoveryClient.Yaml;

/// <summary>
/// A source that reads a service catalogue from YAML.
/// </summary>
/// <remarks>
/// The stream is supplied by a callback rather than passed in, so the source can be constructed
/// during dependency injection setup while the file is only opened when the catalogue is actually
/// read — and read again, from a fresh stream, if it ever is.
/// </remarks>
public class YamlServiceDefinitionsSource : IServiceDefinitionsSource
{
    /// <summary>
    /// Opens the YAML to read.
    /// </summary>
    private readonly Func<Stream?> supplier;

    /// <summary>
    /// Creates a new source over a stream supplier.
    /// </summary>
    /// <param name="supplier">
    /// Opens the YAML to read. Called each time the catalogue is read, and the stream it returns is
    /// disposed afterwards.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="supplier"/> is null.</exception>
    public YamlServiceDefinitionsSource(Func<Stream?> supplier)
    {
        ArgumentNullException.ThrowIfNull(supplier);

        this.supplier = supplier;
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    /// Thrown when the supplier yields no stream.
    /// </exception>
    /// <exception cref="YamlDotNet.Core.YamlException">
    /// Thrown when the YAML is malformed, or a value cannot be converted to the type the model
    /// declares — a port that is not a number, say.
    /// </exception>
    public virtual ServiceDefinitions? GetDefinitions()
    {
        using var stream = this.supplier.Invoke()
            ?? throw new InvalidOperationException("The YAML stream supplier returned no stream, so there is no catalogue to read.");

        using var reader = new StreamReader(stream);

        var deserializer = new DeserializerBuilder()

            // The catalogue's keys are lower-case, and every property in the model is a single word,
            // so this convention maps them one to one. Changing it would silently stop matching the
            // keys in every existing catalogue.
            .WithNamingConvention(HyphenatedNamingConvention.Instance)

            // A key the model does not know is ignored rather than fatal, so a catalogue can carry
            // annotations, or a field a newer version of this library reads, without stopping a host
            // that does not understand it. The cost is that a misspelled key — `port:` for `ports:` —
            // is silently skipped instead of reported.
            .IgnoreUnmatchedProperties()
            .Build();

        // An empty document deserializes to null, which is a catalogue declaring nothing rather than
        // an error: an operator who mounted an empty file gets no services, not a host that will not
        // start.
        return deserializer.Deserialize<ServiceDefinitions>(reader) ?? new ServiceDefinitions();
    }
}
