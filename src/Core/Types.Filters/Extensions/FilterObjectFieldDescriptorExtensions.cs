using System;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Filters
{
    public class FilterObjectFieldDescriptorExtensions
    {
        public static IObjectFieldDescriptor UsePaging<TFilter>(
            this IObjectFieldDescriptor descriptor)
            where TFilter : class, IFilterInputType
        {
            FieldMiddleware placeholder =
                next => context => Task.CompletedTask;
            Type middlewareDefinition = typeof(QueryableConnectionMiddleware<>);

            descriptor

                .Use(placeholder)
                .Extend()
                .OnBeforeCompletion((context, defintion) =>
                {
                    var reference = new ClrTypeReference(
                        typeof(IInputType),
                        TypeContext.Input);
                    IFilterInputType type = context.GetType<IFilterInputType>(reference);

                    Type middlewareType = middlewareDefinition
                        .MakeGenericType(hasClrType.ClrType);
                    FieldMiddleware middleware =
                        FieldClassMiddlewareFactory.Create(middlewareType);
                    int index =
                        defintion.MiddlewareComponents.IndexOf(placeholder);
                    defintion.MiddlewareComponents[index] = middleware;

                })
                .DependsOn<TSchemaType>();

            return descriptor;
        }

        public static IObjectFieldDescriptor AddFilterArguments<TFilter>(
            this IObjectFieldDescriptor descriptor)
            where TFilter : class, IInputType, IFilterInputType
        {
            return descriptor.Argument("where", a => a.Type<TFilter>());
        }

        public static IInterfaceFieldDescriptor AddFilterArguments<TFilter>(
            this IInterfaceFieldDescriptor descriptor)
            where TFilter : class, IInputType, IFilterInputType
        {
            return descriptor.Argument("where", a => a.Type<TFilter>());
        }
    }
}
