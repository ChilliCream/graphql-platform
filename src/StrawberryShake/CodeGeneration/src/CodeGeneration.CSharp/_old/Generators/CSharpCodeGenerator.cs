namespace StrawberryShake.CodeGeneration.CSharp
{
    public abstract class CSharpCodeGenerator<TDescriptor>
        : CodeGenerator<TDescriptor>
        where TDescriptor : ICodeDescriptor
    {
        protected bool NullableRefTypes { get; } = true;

        protected WellKnownTypes Types { get; } = new WellKnownTypes();
    }

}
