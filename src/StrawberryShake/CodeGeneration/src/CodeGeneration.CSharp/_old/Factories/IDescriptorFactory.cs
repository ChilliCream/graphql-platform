namespace StrawberryShake.CodeGeneration.CSharp
{
    public interface IDescriptorFactory<in TModel, out TDescriptor>
        where TDescriptor : ICodeDescriptor
    {
        TDescriptor Create(ICSharpClientBuilderContext context, TModel model);
    }
}
