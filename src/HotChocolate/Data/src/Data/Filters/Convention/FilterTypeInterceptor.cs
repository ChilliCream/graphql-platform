using System;
using HotChocolate.Types.Descriptors;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;
using System.Linq;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Data.Filters
{
    public class FilterTypeInterceptor
        : TypeInterceptor
    {
        private readonly Dictionary<string, IFilterConvention> _conventions
            = new Dictionary<string, IFilterConvention>();

        public override bool CanHandle(ITypeSystemObjectContext context) => true;

        public override void OnBeforeRegisterDependencies(
            ITypeDiscoveryContext discoveryContext,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
            if (definition is FilterInputTypeDefinition def)
            {
                IFilterConvention? convention = GetConvention(
                    discoveryContext.DescriptorContext,
                    def.Scope);

                var descriptor = FilterInputTypeDescriptor.From(
                    discoveryContext.DescriptorContext,
                    def,
                    def.Scope);

                SchemaTypeReference? schemaTypeReference = TypeReference.Create(
                    discoveryContext.Type,
                    def.Scope);

                IEnumerable<Action<IFilterInputTypeDescriptor>> extensions =
                    convention.GetExtensions(schemaTypeReference, def.Name);

                if (extensions.Any())
                {
                    foreach (Action<IFilterInputTypeDescriptor>? extension in extensions)
                    {
                        extension.Invoke(descriptor);
                    }

                    descriptor.CreateDefinition();
                }

                foreach (InputFieldDefinition field in def.Fields)
                {
                    if (field is FilterFieldDefinition filterFieldDefinition)
                    {
                        if (ShouldScopeTypeOfField(filterFieldDefinition))
                        {
                            field.Type = field.Type.With(scope: discoveryContext.Scope);
                        }

                        if (convention.TryGetHandler(
                                discoveryContext,
                                def,
                                filterFieldDefinition,
                                out FilterFieldHandler? handler))
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

        // TODO: This is just a temporary workaraound to avoid scopeing scalars
        public bool ShouldScopeTypeOfField(FilterFieldDefinition definition)
        {
            switch (definition.Type)
            {
                case ClrTypeReference clrRef:
                    if (!BaseTypes.IsNonGenericBaseType(clrRef.Type)
                        && TypeInspector.Default.TryCreate(clrRef.Type, out TypeInfo typeInfo))
                    {
                        return !typeof(ScalarType).IsAssignableFrom(typeInfo.ClrType);
                    }
                    return true;
                case SchemaTypeReference schemaRef:
                    return !typeof(ScalarType).IsAssignableFrom(schemaRef.Type.GetType());
                default:
                    return true;
            };
        }

        public override void OnBeforeCompleteName(
            ITypeCompletionContext completionContext,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
            if (definition is FilterInputTypeDefinition def &&
                def.Scope != ConventionBase.DefaultScope)
            {
                definition.Name = completionContext?.Scope +
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
