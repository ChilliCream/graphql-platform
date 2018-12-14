namespace HotChocolate.Types
{
    public interface IComplexOutputType
        : INamedOutputType
    {
        IDirectiveCollection Directives { get; }

        IFieldCollection<IOutputField> Fields { get; }
    }
}
