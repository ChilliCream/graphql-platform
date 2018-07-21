using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Runtime
{
    // TODO : move runtime namespace into separate lib
    public delegate Task TriggerDataLoaderAsync(
        object dataLoader,
        CancellationToken cancellationToken);

    public class DataLoaderDescriptor
        : IScopedStateDescriptor<string>
    {
        public DataLoaderDescriptor(
            string key,
            Type type,
            ExecutionScope scope,
            Func<IServiceProvider, object> factory,
            TriggerDataLoaderAsync triggerLoadAsync)
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

        public TriggerDataLoaderAsync TriggerLoadAsync { get; }
    }
}
