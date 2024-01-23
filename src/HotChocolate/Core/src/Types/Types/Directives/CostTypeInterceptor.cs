using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Pagination;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types;

internal class CostTypeInterceptor : TypeInterceptor
{
    private const string _connectionType =
        "HotChocolate.Types.Pagination.Connection";
    private const string _complexitySettings =
        "HotChocolate.Execution.Options.ComplexityAnalyzerSettings";
    private bool _pagingSettingsResolved;
    private bool _costSettingsResolved;
    private PagingOptions _pagingOptions;
    private ICostSettings _costSettings = default!;

    public override void OnBeforeRegisterDependencies(
        ITypeDiscoveryContext discoveryContext,
        DefinitionBase definition)
    {
        EnsurePagingSettingsAreLoaded(discoveryContext.DescriptorContext);
        EnsureCostSettingsAreLoaded(discoveryContext.DescriptorContext);

        if (!_costSettings.Enable)
        {
            return;
        }

        // if the cost settings are set to apply default cost we need to ensure that
        // object types that we apply defaults to have type dependencies to the
        // cost directive.
        if (_costSettings.ApplyDefaults &&
            !discoveryContext.IsIntrospectionType &&
            definition is ObjectTypeDefinition objectDef &&
            objectDef.Fields.Any(CanApplyDefaultCost))
        {
            var directive =
                discoveryContext.TypeInspector.GetType(typeof(CostDirectiveType));

            discoveryContext.Dependencies.Add(new(
                TypeReference.Create(directive),
                TypeDependencyFulfilled.Completed));
        }
    }

    public override void OnBeforeCompleteType(
        ITypeCompletionContext completionContext,
        DefinitionBase definition)
    {
        if (!_costSettings.Enable || !_costSettings.ApplyDefaults)
        {
            return;
        }

        if (!completionContext.IsIntrospectionType &&
            definition is ObjectTypeDefinition objectDef)
        {
            foreach (var field in objectDef.Fields)
            {
                if (CanApplyDefaultCost(field))
                {
                    if (IsConnection(field))
                    {
                        ApplyConnectionCosts(field);
                    }
                    else if (IsDataResolver(field))
                    {
                        ApplyDataResolverCosts(field);
                    }
                }
            }
        }
    }

    private void ApplyConnectionCosts(ObjectFieldDefinition field)
    {
        var multipliers = new ListValueNode(
            new StringValueNode("first"),
            new StringValueNode("last"));

        var defaultMultiplier = _pagingOptions.DefaultPageSize ?? 10;

        field.Directives.Add(
            new DirectiveDefinition(
                new DirectiveNode(
                    "cost",
                    new ArgumentNode("complexity", _costSettings.DefaultResolverComplexity),
                    new ArgumentNode("multipliers", multipliers),
                    new ArgumentNode("defaultMultiplier", defaultMultiplier))));
    }

    private void ApplyDataResolverCosts(ObjectFieldDefinition field)
    {
        field.Directives.Add(
            new DirectiveDefinition(
                new DirectiveNode(
                    "cost",
                    new ArgumentNode("complexity", _costSettings.DefaultResolverComplexity))));
    }

    private bool CanApplyDefaultCost(ObjectFieldDefinition field)
    {
        if (field.IsIntrospectionField)
        {
            return false;
        }

        var directives = field.GetDirectives();
        return directives is { Count: 0, } || !directives.Any(IsCostDirective);
    }

    private static bool IsCostDirective(DirectiveDefinition directive)
    {
        if (directive.Type is NameDirectiveReference { Name: "cost", })
        {
            return true;
        }

        if (directive.Type is ExtendedTypeDirectiveReference { Type.Type: { } runtimeType, } &&
            runtimeType == typeof(CostDirective))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Defines if a resolver is possible fetching data and causing higher impact on the system.
    /// </summary>
    internal static bool IsDataResolver(ObjectFieldDefinition field)
    {
        if (field.PureResolver is not null && field.MiddlewareDefinitions.Count == 0)
        {
            return false;
        }

        if (field.Resolver is not null)
        {
            return true;
        }

        var resolver = field.ResolverMember ?? field.Member;

        if (resolver is MethodInfo method)
        {
            if (typeof(Task).IsAssignableFrom(method.ReturnType) ||
                typeof(IQueryable).IsAssignableFrom(method.ReturnType) ||
                typeof(IExecutable).IsAssignableFrom(method.ReturnType))
            {
                return true;
            }

            if (method.ReturnType.IsGenericType &&
                method.ReturnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsConnection(ObjectFieldDefinition field) =>
        field.GetCustomSettings()
            .OfType<Type>()
            .Any(t => t.FullName.EqualsOrdinal(_connectionType));

    private void EnsurePagingSettingsAreLoaded(IDescriptorContext descriptorContext)
    {
        if (!_pagingSettingsResolved)
        {
            _pagingOptions = descriptorContext.GetSettings(_pagingOptions);
            _pagingSettingsResolved = true;
        }
    }

    private void EnsureCostSettingsAreLoaded(IDescriptorContext descriptorContext)
    {
        if (!_costSettingsResolved)
        {
            _costSettings =
                descriptorContext.ContextData.TryGetValue(_complexitySettings, out var value) &&
                value is ICostSettings costSettings
                    ? costSettings
                    : new DefaultCostSettings();
            _costSettingsResolved = true;
        }
    }

    private sealed class DefaultCostSettings : ICostSettings
    {
        public bool Enable => false;
        public bool ApplyDefaults => false;
        public int DefaultComplexity => 1;
        public int DefaultResolverComplexity => 5;
    }
}
