namespace StrawberryShake.CodeGeneration.CSharp
{
    public abstract partial class TypeMapperGenerator: ClassBaseGenerator<ITypeDescriptor>
    {
        protected const string StoreFieldName = "_entityStore";
    }
}
