using System;
using System.Threading.Tasks;
using HotChocolate.Internal;
using HotChocolate.Resolvers;

namespace HotChocolate.Configuration
{
    internal class DataLoaderDescriptor
        : IStateObjectDescriptor<string>
    {
        public DataLoaderDescriptor(
            string key,
            Type type,
            ExecutionScope scope,
            Func<IServiceProvider, object> factory,
            Func<object, Task> triggerLoadAsync)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Scope = scope;
            Factory = factory;
            TriggerLoadAsync = triggerLoadAsync;
        }

        public string Key { get; }

        public Type Type { get; }

        public ExecutionScope Scope { get; }

        public Func<IServiceProvider, object> Factory { get; }

        public Func<object, Task> TriggerLoadAsync { get; }
    }
}
