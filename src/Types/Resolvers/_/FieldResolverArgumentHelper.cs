using System;
using System.Reflection;
using System.Threading;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Resolvers
{
    internal static class FieldResolverArgumentHelper
    {
        internal static ArgumentKind LookupKind(
           ParameterInfo parameter, Type sourceType)
        {
            ArgumentKind argumentKind;
            if (TryCheckForResolverArguments(
                    parameter, sourceType, out argumentKind)
                || TryCheckForSchemaTypes(parameter, out argumentKind)
                || TryCheckForQueryTypes(parameter, out argumentKind)
                || TryCheckForExtensions(parameter, out argumentKind))
            {
                return argumentKind;
            }

            return ArgumentKind.Argument;
        }

        private static bool TryCheckForResolverArguments(
            this ParameterInfo parameter,
            Type sourceType,
            out ArgumentKind argumentKind)
        {
            if (parameter.ParameterType == sourceType)
            {
                argumentKind = ArgumentKind.Source;
                return true;
            }

            if (parameter.ParameterType == typeof(IResolverContext))
            {
                argumentKind = ArgumentKind.Context;
                return true;
            }

            if (parameter.ParameterType == typeof(CancellationToken))
            {
                argumentKind = ArgumentKind.CancellationToken;
                return true;
            }

            argumentKind = default(ArgumentKind);
            return false;
        }

        private static bool TryCheckForSchemaTypes(
            this ParameterInfo parameter,
            out ArgumentKind argumentKind)
        {
            if (parameter.ParameterType == typeof(ISchema))
            {
                argumentKind = ArgumentKind.Schema;
                return true;
            }

            if (parameter.ParameterType == typeof(Schema))
            {
                argumentKind = ArgumentKind.Schema;
                return true;
            }

            if (parameter.ParameterType == typeof(ObjectType))
            {
                argumentKind = ArgumentKind.ObjectType;
                return true;
            }

            if (parameter.ParameterType == typeof(IOutputField))
            {
                argumentKind = ArgumentKind.Field;
                return true;
            }

            if (parameter.ParameterType == typeof(ObjectField))
            {
                argumentKind = ArgumentKind.Field;
                return true;
            }

            argumentKind = default(ArgumentKind);
            return false;
        }

        private static bool TryCheckForQueryTypes(
            this ParameterInfo parameter,
            out ArgumentKind argumentKind)
        {
            if (parameter.ParameterType == typeof(DocumentNode))
            {
                argumentKind = ArgumentKind.QueryDocument;
                return true;
            }

            if (parameter.ParameterType == typeof(OperationDefinitionNode))
            {
                argumentKind = ArgumentKind.OperationDefinition;
                return true;
            }

            if (parameter.ParameterType == typeof(FieldNode))
            {
                argumentKind = ArgumentKind.FieldSelection;
                return true;
            }

            argumentKind = default(ArgumentKind);
            return false;
        }

        private static bool TryCheckForExtensions(
            this ParameterInfo parameter,
            out ArgumentKind argumentKind)
        {
            if (parameter.IsDataLoader())
            {
                argumentKind = ArgumentKind.DataLoader;
                return true;
            }

            if (parameter.IsState())
            {
                argumentKind = ArgumentKind.CustomContext;
                return true;
            }

            if (parameter.IsService())
            {
                argumentKind = ArgumentKind.Service;
                return true;
            }

            argumentKind = default(ArgumentKind);
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
    }
}
