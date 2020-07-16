using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using System.Linq;
using System.Collections.Generic;
using HotChocolate.Utilities;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Filters
{
    public class FilterTypeInterceptor
        : TypeInterceptor
        , ITypeScopeInterceptor
    {
        private readonly Dictionary<string, IFilterConvention> _conventions
            = new Dictionary<string, IFilterConvention>();

        public override bool CanHandle(
            ITypeSystemObjectContext context) =>
            context is { Scope: { } };

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


                foreach (InputFieldDefinition field in def.Fields)
                {
                    if (field.Type.Scope is null)
                    {
                        field.Type = field.Type.With(scope: discoveryContext.Scope);
                    }
                }
            }
        }

        public override void OnBeforeCompleteName(
            ITypeCompletionContext completionContext,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
            definition.Name = completionContext.Scope +
                "_" +
                definition.Name;
        }

        public bool TryCreateScope(ITypeDiscoveryContext discoveryContext, [NotNullWhen(true)] out IReadOnlyList<TypeDependency> typeDependencies)
        {
            throw new NotImplementedException();
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
