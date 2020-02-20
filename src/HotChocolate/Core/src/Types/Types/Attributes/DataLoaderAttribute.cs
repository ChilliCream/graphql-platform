using System;
using System.Reflection;
using GreenDonut;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types
{
    public abstract class DataLoaderAttribute
        : DescriptorAttribute
    {
        public string? Name { get; set; }

        protected abstract Type Type { get; }

        protected abstract IDataLoader CreateDataLoader(IResolverContext context);

        protected internal override void TryConfigure(
            IDescriptorContext context,
            IDescriptor descriptor,
            ICustomAttributeProvider element)
        {
            if (element is MemberInfo member)
            {
                if (descriptor is IObjectFieldDescriptor objectField)
                {
                    IOutputFieldTransformation transformation =
                        CreateTransformation(context, objectField, member);

                    objectField
                        .RewriteType(member, transformation.ResultType)
                        .Extend()
                        .OnBeforeCreate(definition =>
                            definition.MiddlewareComponents.Insert(
                                0,
                                next => async context =>
                                {
                                    IDataLoader dataLoader = CreateDataLoader(context);

                                }));
                }
                else if (descriptor is IInterfaceFieldDescriptor interfaceField)
                {
                    IOutputFieldTransformation transformation =
                        CreateTransformation(context, interfaceField, member);

                    interfaceField
                        .RewriteType(member, transformation.ResultType);
                }
            }
        }
    }
}
