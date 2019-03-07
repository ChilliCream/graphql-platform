using System;
namespace HotChocolate.Types.Descriptors
{
    public interface IDescriptorContext
    {
        INamingConventions Naming { get; }

        ITypeInspector Inspector { get; }
    }

    internal sealed class DescriptorContext
        : IDescriptorContext
    {
        public INamingConventions Naming => throw new System.NotImplementedException();

        public ITypeInspector Inspector => throw new System.NotImplementedException();

        public static DescriptorContext Create(IServiceProvider services)
        {
            throw new System.NotImplementedException();
        }
    }
}
