using System;

namespace HotChocolate.Types
{
    public class Resource
    {
        protected abstract void Configure()
        {

        }
    }

    public interface IResourceDescriptor
    {
        IResourceDescriptor Service(Type type);

        IResourceDescriptor DataLoader(Type type);

        IResourceDescriptor DataLoader(string name);
    }
}
