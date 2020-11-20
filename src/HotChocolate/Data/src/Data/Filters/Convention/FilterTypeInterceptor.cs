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

        public override void OnBeforeCompleteName(
            ITypeCompletionContext completionContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
            if (definition is FilterInputTypeDefinition def)
            {
                IFilterConvention convention = GetConvention(
                    completionContext.DescriptorContext,
                    def.Scope);

                var descriptor = FilterInputTypeDescriptor.New(
                    completionContext.DescriptorContext,
                    def.EntityType,
                    def.Scope);

                var typeReference = TypeReference.Create(
                    completionContext.Type,
                    def.Scope);

                convention.ApplyConfigurations(typeReference, descriptor);

                FilterTypeExtensionHelper.MergeFilterInputTypeDefinitions(
                    completionContext,
                    descriptor.CreateDefinition(),
                    def);

                foreach (InputFieldDefinition field in def.Fields)
                {
                    if (field is FilterFieldDefinition filterFieldDefinition)
                    {
                        if (completionContext.TryPredictTypeKind(
                            filterFieldDefinition.Type,
                            out TypeKind kind) &&
                            kind != TypeKind.Scalar && kind != TypeKind.Enum)
                        {
                            field.Type = field.Type.With(scope: completionContext.Scope);
                        }

                        if (filterFieldDefinition.Handler is null)
                        {
                            if (convention.TryGetHandler(
                                completionContext,
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


                if (def.Scope is not null)
                {
                    definition.Name = completionContext.Scope +
                        "_" +
                        definition.Name;
                }
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
