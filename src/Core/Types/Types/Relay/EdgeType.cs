using System;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Relay
{
    public class EdgeType<T>
        : ObjectType<IEdge>
        , IEdgeType
        where T : IOutputType, new()
    {
        public IOutputType EntityType { get; private set; }

        protected override void Configure(
            IObjectTypeDescriptor<IEdge> descriptor)
        {
            if (!NamedTypeInfoFactory.Default.TryExtractName(
                typeof(T), out NameString name))
            {
                throw new InvalidOperationException(
                    $"Unable to extract a name from {typeof(T).FullName}.");
            }

            descriptor.Description("An edge in a connection.");

            descriptor.BindFields(BindingBehavior.Explicit);

            descriptor.Field(t => t.Cursor)
                .Name("cursor")
                .Description("A cursor for use in pagination.")
                .Type<NonNullType<StringType>>();

            descriptor.Field(t => t.Node)
                .Name("node")
                .Description("The item at the end of the edge.")
                .Type<T>();
        }

        protected override void OnRegisterDependencies(
            IInitializationContext context,
            ObjectTypeDefinition definition)
        {
            base.OnRegisterDependencies(context, definition);

            context.RegisterDependency(
                new ClrTypeReference(
                    typeof(T),
                    TypeContext.Output),
                TypeDependencyKind.Named);
        }

        protected override void OnCompleteName(
            ICompletionContext context,
            ObjectTypeDefinition definition)
        {
            base.OnCompleteName(context, definition);

            INamedType namedType = context.GetType<INamedType>(
                new ClrTypeReference(typeof(T), TypeContext.Output));

            Name = namedType.Name + "Edge";
        }

        protected override void OnCompleteType(
            ICompletionContext context,
            ObjectTypeDefinition definition)
        {
            base.OnCompleteType(context, definition);

            EntityType = context.GetType<T>(
                new ClrTypeReference(
                    typeof(T),
                    TypeContext.Output));
        }
    }
}
