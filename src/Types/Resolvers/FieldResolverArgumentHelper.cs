using System;
using System.Reflection;
using System.Threading;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Resolvers
{
    internal static class FieldResolverArgumentHelper
    {
        internal static FieldResolverArgumentKind LookupKind(
           ParameterInfo parameter, Type sourceType)
        {
            FieldResolverArgumentKind argumentKind;
            if (TryCheckForResolverArguments(
                    parameter, sourceType, out argumentKind)
                || TryCheckForSchemaTypes(parameter, out argumentKind)
                || TryCheckForQueryTypes(parameter, out argumentKind)
                || TryCheckForExtensions(parameter, out argumentKind))
            {
                return argumentKind;
            }

            return FieldResolverArgumentKind.Argument;
        }

        private static bool TryCheckForResolverArguments(
            this ParameterInfo parameter,
            Type sourceType,
            out FieldResolverArgumentKind argumentKind)
        {
            if (parameter.ParameterType == sourceType || parameter.IsParent())
            {
                argumentKind = FieldResolverArgumentKind.Source;
                return true;
            }

            if (parameter.ParameterType == typeof(IResolverContext))
            {
                argumentKind = FieldResolverArgumentKind.Context;
                return true;
            }

            if (parameter.ParameterType == typeof(CancellationToken))
            {
                argumentKind = FieldResolverArgumentKind.CancellationToken;
                return true;
            }

            argumentKind = default(FieldResolverArgumentKind);
            return false;
        }

        private static bool TryCheckForSchemaTypes(
            this ParameterInfo parameter,
            out FieldResolverArgumentKind argumentKind)
        {
            if (typeof(ISchema).IsAssignableFrom(parameter.ParameterType))
            {
                argumentKind = FieldResolverArgumentKind.Schema;
                return true;
            }

            if (parameter.ParameterType == typeof(ObjectType))
            {
                argumentKind = FieldResolverArgumentKind.ObjectType;
                return true;
            }

            if (typeof(IOutputField).IsAssignableFrom(parameter.ParameterType))
            {
                argumentKind = FieldResolverArgumentKind.Field;
                return true;
            }

            argumentKind = default(FieldResolverArgumentKind);
            return false;
        }

        private static bool TryCheckForQueryTypes(
            this ParameterInfo parameter,
            out FieldResolverArgumentKind argumentKind)
        {
            if (parameter.ParameterType == typeof(DocumentNode))
            {
                argumentKind = FieldResolverArgumentKind.QueryDocument;
                return true;
            }

            if (parameter.ParameterType == typeof(OperationDefinitionNode))
            {
                argumentKind = FieldResolverArgumentKind.OperationDefinition;
                return true;
            }

            if (parameter.ParameterType == typeof(FieldNode))
            {
                argumentKind = FieldResolverArgumentKind.FieldSelection;
                return true;
            }

            argumentKind = default(FieldResolverArgumentKind);
            return false;
        }

        private static bool TryCheckForExtensions(
            this ParameterInfo parameter,
            out FieldResolverArgumentKind argumentKind)
        {
            if (parameter.IsDataLoader())
            {
                argumentKind = FieldResolverArgumentKind.DataLoader;
                return true;
            }

            if (parameter.IsState())
            {
                argumentKind = FieldResolverArgumentKind.CustomContext;
                return true;
            }

            if (parameter.IsService())
            {
                argumentKind = FieldResolverArgumentKind.Service;
                return true;
            }

            argumentKind = default(FieldResolverArgumentKind);
            return false;
        }

        private static bool IsDataLoader(this ParameterInfo parameter)
        {
            return parameter.IsDefined(typeof(DataLoaderAttribute));
        }

        private static bool IsState(this ParameterInfo parameter)
        {
            return parameter.IsDefined(typeof(StateAttribute));
        }

        private static bool IsService(this ParameterInfo parameter)
        {
            return parameter.IsDefined(typeof(ServiceAttribute));
        }

        private static bool IsParent(this ParameterInfo parameter)
        {
            return parameter.IsDefined(typeof(ParentAttribute));
        }
    }
}
