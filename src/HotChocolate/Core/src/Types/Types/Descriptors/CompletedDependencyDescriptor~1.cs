using System;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Descriptors
{
    internal class CompletedDependencyDescriptor<T>
        : DependencyDescriptorBase<T>
        , ICompletedDependencyDescriptor
        where T : DefinitionBase
    {
        public CompletedDependencyDescriptor(
            ITypeInspector typeInspector,
            TypeConfiguration<T> configuration)
            : base(typeInspector, configuration)
        {
        }

        protected override TypeDependencyKind DependencyKind =>
            TypeDependencyKind.Completed;

        public ICompletedDependencyDescriptor DependsOn<TType>()
            where TType : ITypeSystemMember =>
            DependsOn<TType>(false);

        public new ICompletedDependencyDescriptor DependsOn<TType>(
            bool mustBeCompleted)
            where TType : ITypeSystemMember
        {
            base.DependsOn<TType>(mustBeCompleted);
            return this;
        }

        public ICompletedDependencyDescriptor DependsOn(Type schemaType) =>
            DependsOn(schemaType, false);

        public new ICompletedDependencyDescriptor DependsOn(
            Type schemaType, bool mustBeCompleted)
        {
            base.DependsOn(schemaType, mustBeCompleted);
            return this;
        }

        public ICompletedDependencyDescriptor DependsOn(
            NameString typeName) =>
            DependsOn(typeName, false);

        public new ICompletedDependencyDescriptor DependsOn(
            NameString typeName,
            bool mustBeCompleted)
        {
            base.DependsOn(typeName, mustBeCompleted);
            return this;
        }
    }
}
