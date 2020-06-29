using System;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.OffsetPaging
{
    public static class OffsetPagingObjectFieldDescriptorExtensions
    {
        internal static IObjectFieldDescriptor UseObjectFieldOffsetPaging(this IObjectFieldDescriptor descriptor, Type schemaType)
        {
            FieldMiddleware placeholder = next => _ => Task.CompletedTask;
            Type middlewareDefinition = typeof(PagingCollectionMiddleware<>);
            var clrTypeReference = new ClrTypeReference(schemaType, TypeContext.Output);

            Type nonGenericType = typeof(CollectionSliceType<>).MakeGenericType(schemaType);

            descriptor.AddOffsetPagingArguments()
                .Type(nonGenericType)
                .Use(placeholder)
                .Extend()
                .OnBeforeCompletion((context, definition) =>
                {
                    IOutputType type = context.GetType<IOutputType>(clrTypeReference);
                    if (type.NamedType() is IHasClrType hasClrType)
                    {
                        Type middlewareType = middlewareDefinition.MakeGenericType(hasClrType.ClrType);
                        FieldMiddleware middleware = FieldClassMiddlewareFactory.Create(middlewareType);
                        int index = definition.MiddlewareComponents.IndexOf(placeholder);
                        definition.MiddlewareComponents[index] = middleware;
                    }
                })
                .DependsOn(nonGenericType);

            return descriptor;
        }

        public static IObjectFieldDescriptor UseOffsetPaging<TType>(this IObjectFieldDescriptor descriptor)
        {
            if (typeof(IOutputType).IsAssignableFrom(typeof(TType)))
                return UseObjectFieldOffsetPaging(descriptor, typeof(TType));

            descriptor
                .AddOffsetPagingArguments()
                .Type<ObjectType<CollectionSlice<TType>>>()
                .Use<PagingCollectionMiddleware<TType>>();

            return descriptor;
        }

        public static IInterfaceFieldDescriptor UseOffsetPaging<TType>(this IInterfaceFieldDescriptor descriptor)
        {
            Type type = typeof(TType);
            if (typeof(IOutputType).IsAssignableFrom(type))
                return UseInterfaceFieldOffsetPaging(descriptor, type);

            descriptor
                .AddOffsetPagingArguments()
                .Type<ObjectType<CollectionSlice<TType>>>();

            return descriptor;
        }

        internal static IInterfaceFieldDescriptor UseInterfaceFieldOffsetPaging(this IInterfaceFieldDescriptor descriptor, Type type)
        {
            Type nonGenericType = typeof(CollectionSliceType<>).MakeGenericType(type);
            return descriptor.AddOffsetPagingArguments().Type(nonGenericType);
        }

        public static IObjectFieldDescriptor AddOffsetPagingArguments(this IObjectFieldDescriptor descriptor)
        {
            return descriptor
                .Argument("take", a => a.Type<PaginationAmountType>())
                .Argument("skip", a => a.Type<PaginationAmountType>());
        }

        public static IInterfaceFieldDescriptor AddOffsetPagingArguments(this IInterfaceFieldDescriptor descriptor)
        {
            return descriptor
                .Argument("take", a => a.Type<PaginationAmountType>())
                .Argument("skip", a => a.Type<PaginationAmountType>());
        }
    }
}