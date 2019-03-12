namespace HotChocolate.Types
{
    public interface IComplexOutputType
        : INamedOutputType
        , IHasDirectives
    {
        IFieldCollection<IOutputField> Fields { get; }
    }
}
