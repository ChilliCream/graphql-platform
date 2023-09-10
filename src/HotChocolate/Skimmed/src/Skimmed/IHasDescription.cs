namespace HotChocolate.Skimmed;

public interface IHasDescription : ITypeSystemMember
{
    /// <summary>
    /// Gets the description of the <see cref="ITypeSystemMember"/>.
    /// </summary>
    string? Description { get; set; }
}