namespace HotChocolate.Types
{
    public interface IComplexOutputType
        : INamedOutputType
    {
        IFieldCollection<IOutputField> Fields { get; }
    }
}
