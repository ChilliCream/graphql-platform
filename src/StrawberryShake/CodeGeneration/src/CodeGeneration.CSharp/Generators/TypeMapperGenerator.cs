using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;

namespace StrawberryShake.CodeGeneration.CSharp.Generators
{
    public abstract partial class TypeMapperGenerator : ClassBaseGenerator<ITypeDescriptor>
    {
        protected const string _entityStore = "_entityStore";
        protected const string entityStore = "entityStore";
    }
}
