using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types
{
    public interface IObjectFieldDescriptor
        : IDescriptor<ObjectFieldDefinition>
        , IFluent
    {
        IObjectFieldDescriptor SyntaxNode(
            FieldDefinitionNode? fieldDefinition);

        IObjectFieldDescriptor Name(
            NameString value);

        IObjectFieldDescriptor Description(
            string? value);

        [Obsolete("Use `Deprecated`.")]
        IObjectFieldDescriptor DeprecationReason(
            string? reason);

        IObjectFieldDescriptor Deprecated(string? reason);

        IObjectFieldDescriptor Deprecated();

        IObjectFieldDescriptor Type<TOutputType>()
            where TOutputType : class, IOutputType;

        IObjectFieldDescriptor Type<TOutputType>(
            TOutputType outputType)
            where TOutputType : class, IOutputType;

        IObjectFieldDescriptor Type(
            ITypeNode typeNode);

        IObjectFieldDescriptor Type(
            Type type);

        IObjectFieldDescriptor Argument(
            NameString argumentName,
            Action<IArgumentDescriptor> argumentDescriptor);

        IObjectFieldDescriptor Ignore(bool ignore = true);

        [Obsolete("Use Resolve(...)")]
        IObjectFieldDescriptor Resolver(
            FieldResolverDelegate fieldResolver);

        [Obsolete("Use Resolve(...)")]
        IObjectFieldDescriptor Resolver(
            FieldResolverDelegate fieldResolver,
            Type resultType);

        IObjectFieldDescriptor Resolve(
            FieldResolverDelegate fieldResolver);

        IObjectFieldDescriptor Resolve(
            FieldResolverDelegate fieldResolver,
            Type resultType);

        IObjectFieldDescriptor ResolveWith<TResolver>(
            Expression<Func<TResolver, object>> propertyOrMethod);

        IObjectFieldDescriptor ResolveWith(MemberInfo propertyOrMethod);

        IObjectFieldDescriptor Subscribe(
            SubscribeResolverDelegate subscribeResolver);

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

        IObjectFieldDescriptor ConfigureContextData(Action<ExtensionData> configure);
    }
}
