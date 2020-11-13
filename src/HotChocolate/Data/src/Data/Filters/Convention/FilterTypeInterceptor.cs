using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Filters
{
    public class FilterTypeInterceptor
        : TypeInterceptor
    {
        private readonly Dictionary<string, IFilterConvention> _conventions =
            new Dictionary<string, IFilterConvention>();

        public override bool CanHandle(ITypeSystemObjectContext context) => true;

        public override void OnBeforeRegisterDependencies(
            ITypeDiscoveryContext discoveryContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
            if (definition is FilterInputTypeDefinition def)
            {
                IFilterConvention convention = GetConvention(
                    discoveryContext.DescriptorContext,
                    def.Scope);

                var descriptor = FilterInputTypeDescriptor.From(
                    discoveryContext.DescriptorContext,
                    def,
                    def.Scope);

                var typeReference = TypeReference.Create(
                    discoveryContext.Type,
                    def.Scope);

                convention.ApplyConfigurations(typeReference, descriptor);

                foreach (InputFieldDefinition field in def.Fields)
                {
                    if (field is FilterFieldDefinition filterFieldDefinition)
                    {
                        if (discoveryContext.TryPredictTypeKind(
                            filterFieldDefinition.Type,
                            out TypeKind kind) &&
                            kind != TypeKind.Scalar && kind != TypeKind.Enum)
                        {
                            field.Type = field.Type.With(scope: discoveryContext.Scope);
                        }

                        if (filterFieldDefinition.Handler is null)
                        {
                            if (convention.TryGetHandler(
                                discoveryContext,
                                def,
                                filterFieldDefinition,
                                out IFilterFieldHandler? handler))
                            {
                                filterFieldDefinition.Handler = handler;
                            }
                            else
                            {
                                throw ThrowHelper.FilterInterceptor_NoHandlerFoundForField(
                                    def,
                                    filterFieldDefinition);
                            }
                        }
                    }
                }
            }
        }

        public override void OnBeforeCompleteName(
            ITypeCompletionContext completionContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
            if (definition is FilterInputTypeDefinition def &&
                def.Scope != null)
            {
                definition.Name = completionContext.Scope +
                    "_" +
                    definition.Name;
            }
        }

        private IFilterConvention GetConvention(
            IDescriptorContext context,
            string? scope)
        {
            if (!_conventions.TryGetValue(
                scope ?? "",
                out IFilterConvention? convention))
            {
                convention = context.GetFilterConvention(scope);
                _conventions[scope ?? ""] = convention;
            }

            return convention;
        }
    }
}
