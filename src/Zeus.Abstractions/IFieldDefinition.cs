namespace Zeus.Abstractions
{
    public interface IFieldDefinition
    {
        string Name { get; }

        IType Type { get; }
        
    }
}