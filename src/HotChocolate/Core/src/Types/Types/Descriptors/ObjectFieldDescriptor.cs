using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types.Descriptors
{
    public class ObjectFieldDescriptor
        : OutputFieldDescriptorBase<ObjectFieldDefinition>
        , IObjectFieldDescriptor
    {
        private bool _argumentsInitialized;

        protected ObjectFieldDescriptor(
            IDescriptorContext context,
            NameString fieldName)
            : base(context)
        {
            Definition.Name = fieldName.EnsureNotEmpty(nameof(fieldName));
            Definition.ResultType = typeof(object);
        }

        protected ObjectFieldDescriptor(
            IDescriptorContext context,
            MemberInfo member,
            Type sourceType)
            : this(context, member, sourceType, null)
        {
        }

        protected ObjectFieldDescriptor(
            IDescriptorContext context,
            MemberInfo member,
            Type sourceType,
            Type? resolverType)
            : base(context)
        {
            Definition.Member = member ??
                throw new ArgumentNullException(nameof(member));
            Definition.Name = context.Naming.GetMemberName(
                member,
                MemberKind.ObjectField);
            Definition.Description = context.Naming.GetMemberDescription(
                member,
                MemberKind.ObjectField);
            Definition.Type = context.TypeInspector.GetOutputReturnTypeRef(member);
            Definition.SourceType = sourceType;
            Definition.ResolverType = resolverType == sourceType ? null : resolverType;

            if (context.Naming.IsDeprecated(member, out var reason))
            {
                Deprecated(reason);
            }

            if (member is MethodInfo m)
            {
                Parameters = m
                    .GetParameters()
                    .ToDictionary(t => new NameString(t.Name));
                Definition.ResultType = m.ReturnType;
            }
            else if (member is PropertyInfo p)
            {
                Definition.ResultType = p.PropertyType;
            }
        }

        protected ObjectFieldDescriptor(
            IDescriptorContext context,
            LambdaExpression expression,
            Type sourceType,
            Type? resolverType)
            : base(context)
        {
            Definition.Expression = expression
                ?? throw new ArgumentNullException(nameof(expression));
            Definition.SourceType = sourceType;
            Definition.ResolverType = resolverType;

            MemberInfo member = ReflectionUtils.TryExtractCallMember(expression);

            if (member is { })
            {
                Definition.Name = context.Naming.GetMemberName(
                    member,
                    MemberKind.ObjectField);
                Definition.Description = context.Naming.GetMemberDescription(
                    member,
                    MemberKind.ObjectField);
                Definition.Type = context.TypeInspector.GetOutputReturnTypeRef(member);

                if (context.Naming.IsDeprecated(member, out string? reason))
                {
                    Deprecated(reason);
                }

                if (member is MethodInfo m)
                {
                    Definition.ResultType = m.ReturnType;
                }
                else if (member is PropertyInfo p)
                {
                    Definition.ResultType = p.PropertyType;
                }
            }
            else
            {
                Definition.Type = context.TypeInspector.GetOutputTypeRef(expression.ReturnType);
                Definition.ResultType = expression.ReturnType;
            }
        }

        protected ObjectFieldDescriptor(
            IDescriptorContext context,
            ObjectFieldDefinition definition)
            : base(context)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        protected internal override ObjectFieldDefinition Definition { get; protected set; } =
            new ObjectFieldDefinition();

        protected override void OnCreateDefinition(
            ObjectFieldDefinition definition)
        {
            if (Definition.Member is { })
            {
                Context.TypeInspector.ApplyAttributes(
                    Context,
                    this,
                    Definition.Member);
            }

            base.OnCreateDefinition(definition);

            CompleteArguments(definition);
        }

        private void CompleteArguments(ObjectFieldDefinition definition)
        {
            if (!_argumentsInitialized)
            {
                FieldDescriptorUtilities.DiscoverArguments(
                    Context,
                    definition.Arguments,
                    definition.Member);
                _argumentsInitialized = true;
            }
        }

        public new IObjectFieldDescriptor SyntaxNode(
            FieldDefinitionNode? fieldDefinition)
        {
            base.SyntaxNode(fieldDefinition);
            return this;
        }

        public new IObjectFieldDescriptor Name(NameString value)
        {
            base.Name(value);
            return this;
        }

        public new IObjectFieldDescriptor Description(
            string? value)
        {
            base.Description(value);
            return this;
        }

        [Obsolete("Use `Deprecated`.")]
        public IObjectFieldDescriptor DeprecationReason(string? reason) =>
            Deprecated(reason);

        public new IObjectFieldDescriptor Deprecated(string? reason)
        {
            base.Deprecated(reason);
            return this;
        }

        public new IObjectFieldDescriptor Deprecated()
        {
            base.Deprecated();
            return this;
        }

        public new IObjectFieldDescriptor Type<TOutputType>()
            where TOutputType : class, IOutputType
        {
            base.Type<TOutputType>();
            return this;
        }

        public new IObjectFieldDescriptor Type<TOutputType>(
            TOutputType outputType)
            where TOutputType : class, IOutputType
        {
            base.Type(outputType);
            return this;
        }

        public new IObjectFieldDescriptor Type(ITypeNode typeNode)
        {
            base.Type(typeNode);
            return this;
        }

        public new IObjectFieldDescriptor Type(Type type)
        {
            base.Type(type);
            return this;
        }

        public new IObjectFieldDescriptor Argument(
            NameString argumentName,
            Action<IArgumentDescriptor> argumentDescriptor)
        {
            base.Argument(argumentName, argumentDescriptor);
            return this;
        }

        public new IObjectFieldDescriptor Ignore(bool ignore = true)
        {
            base.Ignore(ignore);
            return this;
        }

        public IObjectFieldDescriptor Resolver(
            FieldResolverDelegate fieldResolver) =>
            Resolve(fieldResolver);

        public IObjectFieldDescriptor Resolver(
            FieldResolverDelegate fieldResolver,
            Type resultType) =>
            Resolve(fieldResolver, resultType);

        public IObjectFieldDescriptor Resolve(
            FieldResolverDelegate fieldResolver)
        {
            if (fieldResolver is null)
            {
                throw new ArgumentNullException(nameof(fieldResolver));
            }

            Definition.Resolver = fieldResolver;
            return this;
        }

        public IObjectFieldDescriptor Resolve(
            FieldResolverDelegate fieldResolver,
            Type resultType)
        {
            if (fieldResolver is null)
            {
                throw new ArgumentNullException(nameof(fieldResolver));
            }

            Definition.Resolver = fieldResolver;

            if (resultType != null)
            {
                Definition.SetMoreSpecificType(
                    Context.TypeInspector.GetType(resultType),
                    TypeContext.Output);

                if (resultType.IsGenericType)
                {
                    Type resultTypeDef = resultType.GetGenericTypeDefinition();

                    Type clrResultType = resultTypeDef == typeof(NativeType<>)
                        ? resultType.GetGenericArguments()[0]
                        : resultType;

                    if (!clrResultType.IsSchemaType())
                    {
                        Definition.ResultType = clrResultType;
                    }
                }
            }

            return this;
        }

        public IObjectFieldDescriptor ResolveWith<TResolver>(
            Expression<Func<TResolver, object>> propertyOrMethod)
        {
            if (propertyOrMethod is null)
            {
                throw new ArgumentNullException(nameof(propertyOrMethod));
            }

            MemberInfo member = propertyOrMethod.ExtractMember();
            if (member is PropertyInfo || member is MethodInfo)
            {
                Type resultType = member.GetReturnType();

                Definition.SetMoreSpecificType(
                    Context.TypeInspector.GetType(resultType),
                    TypeContext.Output);

                Definition.ResolverType = typeof(TResolver);
                Definition.ResolverMember = member;
                Definition.Resolver = null;
                Definition.ResultType = resultType;
                return this;
            }

            throw new ArgumentException(
                TypeResources.ObjectTypeDescriptor_MustBePropertyOrMethod,
                nameof(propertyOrMethod));
        }

        public IObjectFieldDescriptor ResolveWith(
            MemberInfo propertyOrMethod)
        {
            if (propertyOrMethod is null)
            {
                throw new ArgumentNullException(nameof(propertyOrMethod));
            }

            if (propertyOrMethod is PropertyInfo || propertyOrMethod is MethodInfo)
            {
                Definition.ResolverType = propertyOrMethod.DeclaringType;
                Definition.ResolverMember = propertyOrMethod;
                Definition.Resolver = null;
                Definition.ResultType = propertyOrMethod.GetReturnType();
            }

            throw new ArgumentException(
                TypeResources.ObjectTypeDescriptor_MustBePropertyOrMethod,
                nameof(propertyOrMethod));
        }

        public IObjectFieldDescriptor Subscribe(SubscribeResolverDelegate subscribeResolver)
        {
            Definition.SubscribeResolver = subscribeResolver;
            return this;
        }

        public IObjectFieldDescriptor Use(FieldMiddleware middleware)
        {
            if (middleware is null)
            {
                throw new ArgumentNullException(nameof(middleware));
            }

            Definition.MiddlewareComponents.Add(middleware);
            return this;
        }

        public new IObjectFieldDescriptor Directive<T>(T directiveInstance)
            where T : class
        {
            base.Directive(directiveInstance);
            return this;
        }

        public new IObjectFieldDescriptor Directive<T>()
            where T : class, new()
        {
            base.Directive<T>();
            return this;
        }

        public new IObjectFieldDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            base.Directive(name, arguments);
            return this;
        }

        public new IObjectFieldDescriptor ConfigureContextData(
            Action<ExtensionData> configure)
        {
            base.ConfigureContextData(configure);
            return this;
        }

        public static ObjectFieldDescriptor New(
            IDescriptorContext context,
            NameString fieldName) =>
            new ObjectFieldDescriptor(context, fieldName);

        public static ObjectFieldDescriptor New(
            IDescriptorContext context,
            MemberInfo member,
            Type sourceType) =>
            new ObjectFieldDescriptor(context, member, sourceType);

        public static ObjectFieldDescriptor New(
            IDescriptorContext context,
            MemberInfo member,
            Type sourceType,
            Type resolverType) =>
            new ObjectFieldDescriptor(context, member, sourceType, resolverType);

        public static ObjectFieldDescriptor New(
            IDescriptorContext context,
            LambdaExpression expression,
            Type sourceType,
            Type resolverType) =>
            new ObjectFieldDescriptor(context, expression, sourceType, resolverType);

        public static ObjectFieldDescriptor From(
            IDescriptorContext context,
            ObjectFieldDefinition definition) =>
            new ObjectFieldDescriptor(context, definition);
    }
}
