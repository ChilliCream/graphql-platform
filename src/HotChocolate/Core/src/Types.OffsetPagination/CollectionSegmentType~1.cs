using System;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Pagination
{
    public class CollectionSegmentType<T>
        : ObjectType<CollectionSegment>
        where T : class, IOutputType
    {
        public CollectionSegmentType()
        {
        }

        public CollectionSegmentType(
            Action<IObjectTypeDescriptor<CollectionSegment>> configure)
            : base(descriptor =>
            {
                ApplyConfig(descriptor);
                configure(descriptor);
            })
        {
        }

        public IOutputType ItemType { get; private set; } = default!;

        protected override void Configure(IObjectTypeDescriptor<CollectionSegment> descriptor) =>
            ApplyConfig(descriptor);

        protected static void ApplyConfig(IObjectTypeDescriptor<CollectionSegment> descriptor)
        {
            descriptor
                .Name(dependency => $"{dependency.Name}CollectionSegment")
                .DependsOn<T>()
                .BindFieldsExplicitly();

            descriptor
                .Field(i => i.Items)
                .Name("items")
                .Type<ListType<T>>();

            descriptor
                .Field(t => t.Info)
                .Name("pageInfo")
                .Description("Information to aid in pagination.")
                .Type<NonNullType<CollectionSegmentInfoType>>();
        }

        protected override void OnRegisterDependencies(
            ITypeDiscoveryContext context,
            ObjectTypeDefinition definition)
        {
            base.OnRegisterDependencies(context, definition);

            context.RegisterDependency(
                context.TypeInspector.GetTypeRef(typeof(T)),
                TypeDependencyKind.Default);
        }

        protected override void OnCompleteType(
            ITypeCompletionContext context,
            ObjectTypeDefinition definition)
        {
            base.OnCompleteType(context, definition);

            ItemType = context.GetType<T>(
                context.TypeInspector.GetTypeRef(typeof(T)));
        }
    }
}
