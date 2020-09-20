using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Sorting
{
    public class SortTypeInterceptor
        : TypeInterceptor
    {
        private readonly Dictionary<string, ISortConvention> _conventions
            = new Dictionary<string, ISortConvention>();

        public override bool CanHandle(ITypeSystemObjectContext context) => true;

        public override void OnBeforeRegisterDependencies(
            ITypeDiscoveryContext discoveryContext,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
            switch (definition)
            {
                case SortInputTypeDefinition inputDefinition:
                    ConfigureSortInputType(discoveryContext, inputDefinition);
                    break;
                case SortEnumTypeDefinition enumTypeDefinition:
                    ConfigureSortEnumType(discoveryContext, enumTypeDefinition);
                    break;
            }
        }

        private void ConfigureSortInputType(
            ITypeDiscoveryContext discoveryContext,
            SortInputTypeDefinition definition)
        {
            ISortConvention convention = GetConvention(
                discoveryContext.DescriptorContext,
                definition.Scope);

            var descriptor = SortInputTypeDescriptor.From(
                discoveryContext.DescriptorContext,
                definition,
                definition.Scope);

            SchemaTypeReference typeReference = TypeReference.Create(
                discoveryContext.Type,
                definition.Scope);

            convention.ApplyConfigurations(typeReference, descriptor);

            foreach (InputFieldDefinition field in definition.Fields)
            {
                if (field is SortFieldDefinition sortFieldDefinition)
                {
                    if (discoveryContext.TryPredictTypeKind(
                        sortFieldDefinition.Type,
                        out TypeKind kind) &&
                        kind != TypeKind.Enum)
                    {
                        field.Type = field.Type.With(scope: discoveryContext.Scope);
                    }

                    if (convention.TryGetFieldHandler(
                        discoveryContext,
                        definition,
                        sortFieldDefinition,
                        out ISortFieldHandler? handler))
                    {
                        sortFieldDefinition.Handler = handler;
                    }
                    else
                    {
                        throw ThrowHelper.SortInterceptor_NoFieldHandlerFoundForField(
                            definition,
                            sortFieldDefinition);
                    }
                }
            }
        }

        private void ConfigureSortEnumType(
            ITypeDiscoveryContext discoveryContext,
            SortEnumTypeDefinition definition)
        {
            ISortConvention convention = GetConvention(
                discoveryContext.DescriptorContext,
                discoveryContext.Scope);

            ISortEnumTypeDescriptor descriptor = SortEnumTypeDescriptor.From(
                discoveryContext.DescriptorContext,
                definition);

            SchemaTypeReference typeReference = TypeReference.Create(
                discoveryContext.Type,
                discoveryContext.Scope);

            convention.ApplyConfigurations(typeReference, descriptor);

            foreach (var enumValue in definition.Values)
            {
                if (enumValue is SortEnumValueDefinition sortEnumValueDefinition)
                {
                    if (convention.TryGetOperationHandler(
                        discoveryContext,
                        definition,
                        sortEnumValueDefinition,
                        out ISortOperationHandler? handler))
                    {
                        sortEnumValueDefinition.Handler = handler;
                    }
                    else
                    {
                        throw ThrowHelper.SortInterceptor_NoOperationHandlerFoundForValue(
                            definition,
                            sortEnumValueDefinition);
                    }
                }
            }
        }

        public override void OnBeforeCompleteName(
            ITypeCompletionContext completionContext,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
            if (definition is {Name: {}} &&
                definition is IHasScope {Scope: {}} &&
                (definition is SortEnumTypeDefinition ||
                    definition is SortInputTypeDefinition))
            {
                definition.Name = completionContext.Scope + "_" + definition.Name;
            }
        }

        private ISortConvention GetConvention(
            IDescriptorContext context,
            string? scope)
        {
            if (!_conventions.TryGetValue(
                scope ?? string.Empty,
                out ISortConvention? convention))
            {
                convention = context.GetSortConvention(scope);
                _conventions[scope ?? string.Empty] = convention;
            }

            return convention;
        }
    }
}
