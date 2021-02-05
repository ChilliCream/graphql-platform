using System;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Filters
{
    internal static class RelayIdFilterFieldExtensions
    {
        public static IFilterOperationFieldDescriptor ID<TEntityType, TIdType>(
            this IFilterOperationFieldDescriptor descriptor)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            descriptor.Extend()
                .OnBeforeCompletion(
                    (c, d) => AddSerializerToInputField<TEntityType, TIdType>(c, d));

            return descriptor;
        }

        private static void AddSerializerToInputField<TEntityType, TIdType>(
            ITypeCompletionContext completionContext,
            FilterOperationFieldDefinition definition)
        {
            ITypeInspector typeInspector = completionContext.TypeInspector;
            IExtendedType? resultType;

            resultType = typeInspector.GetReturnType(typeof(TIdType[]), true);
            ExtendedTypeReference resultTypeReference =
                typeInspector.GetReturnTypeRef(typeof(TEntityType));

            definition.Formatters.Add(
                CreateSerializer(
                    completionContext,
                    resultType,
                    resultTypeReference));
        }

        private static IInputValueFormatter CreateSerializer(
            ITypeCompletionContext completionContext,
            IExtendedType resultType,
            ExtendedTypeReference extendedTypeReference)
        {
            NameString typeName =
                completionContext.GetType<IType>(extendedTypeReference).TypeName();
            IIdSerializer serializer =
                completionContext.Services.GetService<IIdSerializer>() ??
                new IdSerializer();

            return new GlobalIdInputValueFormatter(
                typeName.HasValue ? typeName : completionContext.Type.Name,
                serializer,
                resultType,
                typeName.HasValue);
        }
    }
}
