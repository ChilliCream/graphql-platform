using System;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    public interface IObjectFieldDescriptor
        : IFluent
    {
        IObjectFieldDescriptor SyntaxNode(FieldDefinitionNode syntaxNode);

        IObjectFieldDescriptor Name(NameString name);

        IObjectFieldDescriptor Description(string description);

        IObjectFieldDescriptor DeprecationReason(string deprecationReason);

        IObjectFieldDescriptor Type<TOutputType>()
            where TOutputType : IOutputType;

        IObjectFieldDescriptor Type<TOutputType>(TOutputType type)
            where TOutputType : class, IOutputType;

        IObjectFieldDescriptor Type(ITypeNode type);

        IObjectFieldDescriptor Argument(NameString name,
            Action<IArgumentDescriptor> argument);

        IObjectFieldDescriptor Ignore();

        IObjectFieldDescriptor Resolver(
            FieldResolverDelegate fieldResolver);

        IObjectFieldDescriptor Resolver(
            FieldResolverDelegate fieldResolver,
            Type resultType);

        IObjectFieldDescriptor Use(FieldMiddleware middleware);

        IObjectFieldDescriptor Directive<T>(T directive)
            where T : class;

        IObjectFieldDescriptor Directive<T>()
            where T : class, new();

        IObjectFieldDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments);
    }
}
