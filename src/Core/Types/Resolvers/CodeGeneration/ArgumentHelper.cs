﻿using System;
using System.Reflection;
using System.Threading;
using GreenDonut;
using HotChocolate.Language;
using HotChocolate.Subscriptions;
using HotChocolate.Types;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal static class ArgumentHelper
    {
        internal static ArgumentKind LookupKind(
           ParameterInfo parameter, Type sourceType)
        {
            if (parameter == null)
            {
                throw new ArgumentNullException(nameof(parameter));
            }

            ArgumentKind argumentKind;
            if (TryCheckForResolverArguments(
                    parameter, sourceType, out argumentKind)
                || TryCheckForSubscription(parameter, out argumentKind)
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

            if (sourceType == null)
            {
                argumentKind = default(ArgumentKind);
                return false;
            }

            if (parameter.ParameterType == sourceType
                || IsParent(parameter, sourceType))
            {
                argumentKind = ArgumentKind.Source;
                return true;
            }

            argumentKind = default(ArgumentKind);
            return false;
        }

        private static bool TryCheckForSchemaTypes(
            this ParameterInfo parameter,
            out ArgumentKind argumentKind)
        {
            if (IsSchema(parameter))
            {
                argumentKind = ArgumentKind.Schema;
                return true;
            }

            if (parameter.ParameterType == typeof(ObjectType))
            {
                argumentKind = ArgumentKind.ObjectType;
                return true;
            }

            if (IsOutputField(parameter))
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
            if (IsDataLoader(parameter))
            {
                argumentKind = ArgumentKind.DataLoader;
                return true;
            }

            if (IsState(parameter))
            {
                argumentKind = ArgumentKind.CustomContext;
                return true;
            }

            if (IsService(parameter))
            {
                argumentKind = ArgumentKind.Service;
                return true;
            }

            argumentKind = default(ArgumentKind);
            return false;
        }

        private static bool TryCheckForSubscription(
            this ParameterInfo parameter,
            out ArgumentKind argumentKind)
        {
            if (IsEventMessage(parameter))
            {
                argumentKind = ArgumentKind.EventMessage;
                return true;
            }

            argumentKind = default(ArgumentKind);
            return false;
        }

        internal static bool IsDataLoader(ParameterInfo parameter)
        {
            return typeof(IDataLoader).IsAssignableFrom(parameter.ParameterType)
                || parameter.IsDefined(typeof(DataLoaderAttribute));
        }

        internal static bool IsState(ParameterInfo parameter)
        {
            return parameter.IsDefined(typeof(StateAttribute));
        }

        internal static bool IsService(ParameterInfo parameter)
        {
            return parameter.IsDefined(typeof(ServiceAttribute));
        }

        internal static bool IsParent(ParameterInfo parameter, Type sourceType)
        {
            return parameter.ParameterType.IsAssignableFrom(sourceType)
                || parameter.IsDefined(typeof(ParentAttribute));
        }

        internal static bool IsEventMessage(ParameterInfo parameter) =>
            typeof(IEventMessage).IsAssignableFrom(parameter.ParameterType);

        internal static bool IsOutputField(ParameterInfo parameter) =>
            typeof(IOutputField).IsAssignableFrom(parameter.ParameterType);

        internal static bool IsSchema(ParameterInfo parameter) =>
            typeof(ISchema).IsAssignableFrom(parameter.ParameterType);
    }
}
