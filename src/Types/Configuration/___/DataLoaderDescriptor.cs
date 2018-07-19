using System;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Configuration
{
    internal class DataLoaderDescriptor
    {
        public DataLoaderDescriptor(string key, Type type, ExecutionScope scope)
            : this(key, type, scope, null)
        {
        }

        public DataLoaderDescriptor(
            string key,
            Type type,
            ExecutionScope scope,
            Func<object, Task> triggerLoadAsync)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Scope = scope;
            TriggerLoadAsync = triggerLoadAsync;
        }

        public string Key { get; }

        public Type Type { get; }

        public ExecutionScope Scope { get; }

        public Func<object, Task> TriggerLoadAsync { get; }
    }
}
