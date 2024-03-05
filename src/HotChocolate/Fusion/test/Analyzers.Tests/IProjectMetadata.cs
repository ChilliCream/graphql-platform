namespace Aspire.Hosting;

public interface IProjectMetadata : IResourceAnnotation
{
    /// <summary>
    /// Gets the fully-qualified path to the project.
    /// </summary>
    public string ProjectPath { get; }
}