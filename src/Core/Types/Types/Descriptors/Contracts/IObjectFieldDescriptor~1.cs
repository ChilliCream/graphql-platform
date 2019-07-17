using System;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    public interface IObjectFieldDescriptor<T>
        : IObjectFieldDescriptor
    {
        new IObjectFieldDescriptor<T> SyntaxNode(
            FieldDefinitionNode fieldDefinition);

        new IObjectFieldDescriptor<T> Name(
            NameString value);

        new IObjectFieldDescriptor<T> Description(
            string value);

        new IObjectFieldDescriptor<T> Deprecated(string reason);

        new IObjectFieldDescriptor<T> Deprecated();

        new IObjectFieldDescriptor<T> Type<TOutputType>()
            where TOutputType : class, IOutputType;

        new IObjectFieldDescriptor<T> Type<TOutputType>(
            TOutputType outputType)
            where TOutputType : class, IOutputType;

        new IObjectFieldDescriptor<T> Type(
            ITypeNode typeNode);

        new IObjectFieldDescriptor<T> Argument(
            NameString argumentName,
            Action<IArgumentDescriptor> argumentDescriptor);

        new IObjectFieldDescriptor<T> Ignore();

        new IObjectFieldDescriptor<T> Resolver(
            FieldResolverDelegate fieldResolver);

        new IObjectFieldDescriptor<T> Resolver(
            FieldResolverDelegate fieldResolver,
            Type resultType);

        new IObjectFieldDescriptor<T> Use(
            FieldMiddleware middleware);

        new IObjectFieldDescriptor<T> Directive<TDirective>(
            TDirective directiveInstance)
            where TDirective : class;

        new IObjectFieldDescriptor<T> Directive<TDirective>()
            where TDirective : class, new();

        new IObjectFieldDescriptor<T> Directive(
            NameString name,
            params ArgumentNode[] arguments);
    }
}
