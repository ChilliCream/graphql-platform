using System;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Descriptors
{
    internal class NamedDependencyDescriptor<T>
        : DependencyDescriptorBase<T>
        , INamedDependencyDescriptor
        where T : DefinitionBase
    {
        public NamedDependencyDescriptor(
            ITypeInspector typeInspector,
            TypeConfiguration<T> configuration)
            : base(typeInspector, configuration)
        {
        }

        protected override TypeDependencyKind DependencyKind =>
            TypeDependencyKind.Named;

        public INamedDependencyDescriptor DependsOn<TType>()
            where TType : ITypeSystemMember =>
            DependsOn<TType>(false);

        public new INamedDependencyDescriptor DependsOn<TType>(bool mustBeNamed)
            where TType : ITypeSystemMember
        {
            base.DependsOn<TType>(mustBeNamed);
            return this;
        }

        public INamedDependencyDescriptor DependsOn(Type schemaType) =>
            DependsOn(schemaType, false);

        public new INamedDependencyDescriptor DependsOn(
            Type schemaType, bool mustBeNamed)
        {
            base.DependsOn(schemaType, mustBeNamed);
            return this;
        }

        public INamedDependencyDescriptor DependsOn(
            NameString typeName) =>
            DependsOn(typeName, false);

        public new INamedDependencyDescriptor DependsOn(
            NameString typeName,
            bool mustBeNamed)
        {
            base.DependsOn(typeName, mustBeNamed);
            return this;
        }
    }
}
