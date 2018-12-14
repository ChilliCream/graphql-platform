using System;

namespace HotChocolate.Runtime
{
    public class CustomContextProvider
        : StateObjectContainer<Type>
        , ICustomContextProvider
    {
        public CustomContextProvider(
            IServiceProvider globalServices,
            IServiceProvider requestServices,
            StateObjectDescriptorCollection<Type> descriptors,
            StateObjectCollection<Type> globalStates)
            : base(globalServices, requestServices, descriptors, globalStates)
        {
        }

        public T GetCustomContext<T>() => (T)GetStateObject(typeof(T));
    }
}
