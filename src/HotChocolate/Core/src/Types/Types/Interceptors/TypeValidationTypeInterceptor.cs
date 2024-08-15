using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types.Interceptors;

internal sealed class TypeValidationTypeInterceptor : TypeInterceptor
{
    public override void OnBeforeRegisterDependencies(
        ITypeDiscoveryContext discoveryContext,
        DefinitionBase definition)
    {
        if (discoveryContext.IsIntrospectionType)
        {
            return;
        }

        switch (definition)
        {
            case ObjectTypeDefinition od:
                ValidateObjectType(discoveryContext, od);
                return;

            case InputObjectTypeDefinition ind:
                ValidateInputObjectType(discoveryContext, ind);
                return;

            case InterfaceTypeDefinition id:
                ValidateInterfaceType(discoveryContext, id);
                return;

            case UnionTypeDefinition ud:
                ValidateUnionType(discoveryContext, ud);
                return;

            case DirectiveTypeDefinition ud:
                ValidateDirectiveType(discoveryContext, ud);
                return;
        }
    }

    private void ValidateInputObjectType(
        ITypeDiscoveryContext context,
        InputObjectTypeDefinition? definition)
    {
        if (definition is { RuntimeType: { } runtimeType, } &&
            IsTypeSystemType(definition.RuntimeType))
        {
            ReportRuntimeTypeError(context, runtimeType);
            definition.RuntimeType = typeof(object);
            definition.Fields.Clear();
            definition.Dependencies.Clear();
        }
    }

    private void ValidateDirectiveType(
        ITypeDiscoveryContext context,
        DirectiveTypeDefinition? definition)
    {
        if (definition is { RuntimeType: { } runtimeType, } &&
            IsTypeSystemType(definition.RuntimeType))
        {
            ReportRuntimeTypeError(context, runtimeType);
            definition.RuntimeType = typeof(object);
            definition.Dependencies.Clear();
        }
    }

    private void ValidateUnionType(
        ITypeDiscoveryContext context,
        UnionTypeDefinition? definition)
    {
        if (definition is { RuntimeType: { } runtimeType, } &&
            IsTypeSystemType(definition.RuntimeType))
        {
            ReportRuntimeTypeError(context, runtimeType);
            definition.RuntimeType = typeof(object);
            definition.Dependencies.Clear();
        }
    }

    private void ValidateObjectType(
        ITypeDiscoveryContext context,
        ObjectTypeDefinition definition)
    {
        if (definition is { RuntimeType: { } runtimeType, } &&
            IsTypeSystemType(definition.RuntimeType))
        {
            ReportRuntimeTypeError(context, runtimeType);
            definition.RuntimeType = typeof(object);
            definition.Fields.Clear();
            definition.Interfaces.Clear();
            definition.Dependencies.Clear();
        }
    }

    private void ValidateInterfaceType(
        ITypeDiscoveryContext context,
        InterfaceTypeDefinition? definition)
    {
        if (definition is { RuntimeType: { } runtimeType, } &&
            IsTypeSystemType(definition.RuntimeType))
        {
            ReportRuntimeTypeError(context, runtimeType);
            definition.RuntimeType = typeof(object);
            definition.Fields.Clear();
            definition.Interfaces.Clear();
            definition.Dependencies.Clear();
        }
    }

    private bool IsTypeSystemType(Type type) =>
        typeof(ITypeSystemMember).IsAssignableFrom(type);

    private void ReportRuntimeTypeError(
        ITypeDiscoveryContext discoveryContext,
        Type runtimeType)
    {
        var schemaError = ErrorHelper
            .NoSchemaTypesAllowedAsRuntimeType(discoveryContext.Type, runtimeType);
        discoveryContext.ReportError(schemaError);
    }
}
