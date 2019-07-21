﻿using System;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public interface IObjectFieldDescriptor
        : IDescriptor<ObjectFieldDefinition>
        , IFluent
    {
        IObjectFieldDescriptor SyntaxNode(
            FieldDefinitionNode fieldDefinition);

        IObjectFieldDescriptor Name(
            NameString value);

        IObjectFieldDescriptor Description(
            string value);

        [Obsolete("Use `Deprecated`.")]
        IObjectFieldDescriptor DeprecationReason(
            string reason);

        IObjectFieldDescriptor Deprecated(string reason);

        IObjectFieldDescriptor Deprecated();

        IObjectFieldDescriptor Type<TOutputType>()
            where TOutputType : class, IOutputType;

        IObjectFieldDescriptor Type<TOutputType>(
            TOutputType outputType)
            where TOutputType : class, IOutputType;

        IObjectFieldDescriptor Type(
            ITypeNode typeNode);

        IObjectFieldDescriptor Argument(
            NameString argumentName,
            Action<IArgumentDescriptor> argumentDescriptor);

        IObjectFieldDescriptor Ignore();

        IObjectFieldDescriptor Resolver(
            FieldResolverDelegate fieldResolver);

        IObjectFieldDescriptor Resolver(
            FieldResolverDelegate fieldResolver,
            Type resultType);

        IObjectFieldDescriptor Use(
            FieldMiddleware middleware);

        IObjectFieldDescriptor Directive<T>(
            T directiveInstance)
            where T : class;

        IObjectFieldDescriptor Directive<T>()
            where T : class, new();

        IObjectFieldDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments);
    }
}
