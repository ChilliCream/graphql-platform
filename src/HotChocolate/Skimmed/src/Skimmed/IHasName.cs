namespace HotChocolate.Skimmed;

public interface IHasName : ITypeSystemMember
{
    string Name { get; set; }
}