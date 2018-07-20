using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HotChocolate.Runtime
{
    // TODO : move runtime namespace into separate lib
    public class DataLoaderDescriptor
        : IScopedStateDescriptor<string>
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

    public interface IDataLoaderState
    {
        /// <summary>
        /// Gets the data loaders that have been requested since the last reset.
        /// </summary>
        /// <value></value>
        IReadOnlyCollection<DataLoaderInfo> Touched { get; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="key"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T GetDataLoader<T>(string key);

        /// <summary>
        /// Resets the touched data loader collection.
        /// </summary>
        void Reset();
    }

    public class DataLoaderInfo
    {
        private readonly DataLoaderDescriptor _descriptor;

        public DataLoaderInfo(DataLoaderDescriptor descriptor, object instance)
        {
            _descriptor = descriptor
                ?? throw new ArgumentNullException(nameof(descriptor));
            Instance = instance
                ?? throw new ArgumentNullException(nameof(instance));
            NeedsToBeTriggered = _descriptor.TriggerLoadAsync != null;
        }

        /// <summary>
        /// The data loader instance.
        /// </summary>
        object Instance { get; }

        /// <summary>
        /// Defines if his data loader has to be triggered
        /// in order for it to load data.
        /// </summary>
        public bool NeedsToBeTriggered { get; }

        /// <summary>
        /// Signals the data loader that all data load registrations have
        /// been made an the batched data retrieval can be started.
        /// </summary>
        public Task TriggerAsync()
        {
            if (NeedsToBeTriggered)
            {
                return _descriptor.TriggerLoadAsync(Instance);
            }
            return Task.CompletedTask;
        }
    }

    public class DataLoaderCollection
        : StateObjectContainer<string>
    {

    }
}
