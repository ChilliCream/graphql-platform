using System;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public interface IDescriptor<T>
        where T : DefinitionBase
    {
        IDescriptorExtension<T> Extend();
    }

    public interface IDescriptorExtension<T>
        where T : DefinitionBase
    {
        void OnBeforeCreate(Action<T> configure);

        IOnBeforeNamingDescriptor OnBeforeNaming(
            Action<ICompletionContext, T> configure);

        IOnBeforeCompletionDescriptor OnBeforeCompletion(
            Action<ICompletionContext, T> configure);
    }

    public interface IOnBeforeNamingDescriptor
    {
        IOnBeforeNamingDescriptor DependsOn<T>()
            where T : ITypeSystem;

        IOnBeforeNamingDescriptor DependsOn<T>(bool mustBeNamed)
            where T : ITypeSystem;

        IOnBeforeNamingDescriptor DependsOn(NameString typeName);

        IOnBeforeNamingDescriptor DependsOn(
            NameString typeName,
            bool mustBeNamed);
    }

    public interface IOnBeforeCompletionDescriptor
    {
        IOnBeforeCompletionDescriptor DependsOn<T>()
            where T : ITypeSystem;

        IOnBeforeCompletionDescriptor DependsOn<T>(bool mustBeNamed)
            where T : ITypeSystem;

        IOnBeforeCompletionDescriptor DependsOn(NameString typeName);

        IOnBeforeCompletionDescriptor DependsOn(
            NameString typeName,
            bool mustBeNamed);
    }




    // Extend().OnBeforeCreate(definition => foo)
    // Extend().OnBeforeNaming((context, definition) => foo).DependsOn<SchemaType>().ToBeNamed()
    // Extend().OnBeforeCompletion((context, definition) => foo).DependsOn<SchemaType>().AsCompleted()
}
