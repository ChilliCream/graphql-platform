using System;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Descriptors
{
    public class ObjectFieldDescriptor<T>
        : ObjectFieldDescriptor
        , IObjectFieldDescriptor<T>
    {
        internal protected ObjectFieldDescriptor(
            IDescriptorContext context,
            NameString fieldName)
            : base(context, fieldName)
        {
        }

        internal protected ObjectFieldDescriptor(
            IDescriptorContext context,
            MemberInfo member)
            : base(context, member)
        {
        }

        internal protected ObjectFieldDescriptor(
            IDescriptorContext context,
            MemberInfo member,
            Type resolverType)
            : base(context, member, resolverType)
        {
        }

        public new IObjectFieldDescriptor<T> SyntaxNode(
            FieldDefinitionNode fieldDefinition)
        {
            base.SyntaxNode(fieldDefinition);
            return this;
        }

        public new IObjectFieldDescriptor<T> Name(NameString value)
        {
            base.Name(value);
            return this;
        }

        public new IObjectFieldDescriptor<T> Description(string value)
        {
            base.Description(value);
            return this;
        }

        public new IObjectFieldDescriptor<T> Deprecated()
        {
            base.Deprecated();
            return this;
        }

        public new IObjectFieldDescriptor<T> Deprecated(string reason)
        {
            base.Description(reason);
            return this;
        }

        public new IObjectFieldDescriptor<T> Type<TOutputType>()
           where TOutputType : class, IOutputType
        {
            base.Type<TOutputType>();
            return this;
        }

        public new IObjectFieldDescriptor<T> Type<TOutputType>(
            TOutputType outputType)
            where TOutputType : class, IOutputType
        {
            base.Type<TOutputType>(outputType);
            return this;
        }

        public new IObjectFieldDescriptor<T> Type(ITypeNode typeNode)
        {
            base.Type(typeNode);
            return this;
        }

        public new IObjectFieldDescriptor<T> Argument(
            NameString argumentName,
            Action<IArgumentDescriptor> argumentDescriptor)
        {
            base.Argument(argumentName, argumentDescriptor);
            return this;
        }

        public new IObjectFieldDescriptor<T> Ignore()
        {
            base.Ignore();
            return this;
        }

        public new IObjectFieldDescriptor<T> Resolver(
            FieldResolverDelegate fieldResolver)
        {
            base.Resolver(fieldResolver);
            return this;
        }

        public new IObjectFieldDescriptor<T> Resolver(
            FieldResolverDelegate fieldResolver,
            Type resultType)
        {
            base.Resolver(fieldResolver, resultType);
            return this;
        }

        public new IObjectFieldDescriptor<T> Use(
            FieldMiddleware middleware)
        {
            base.Use(middleware);
            return this;
        }

        public new IObjectFieldDescriptor<T> Directive<TDirective>(
            TDirective directiveInstance)
            where TDirective : class
        {
            base.Directive<TDirective>(directiveInstance);
            return this;
        }

        public new IObjectFieldDescriptor<T> Directive<TDirective>()
            where TDirective : class, new()
        {
            base.Directive<TDirective>();
            return this;
        }

        public new IObjectFieldDescriptor<T> Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            base.Directive(name, arguments);
            return this;
        }
    }
}
