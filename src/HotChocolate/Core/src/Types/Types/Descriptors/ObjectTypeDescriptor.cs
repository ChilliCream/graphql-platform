using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;
using static HotChocolate.Properties.TypeResources;

namespace HotChocolate.Types.Descriptors
{
    public class ObjectTypeDescriptor
        : DescriptorBase<ObjectTypeDefinition>
        , IObjectTypeDescriptor
    {
        private ICollection<Type>? _resolverTypes;

        protected ObjectTypeDescriptor(IDescriptorContext context, Type clrType)
            : base(context)
        {
            if (clrType is null)
            {
                throw new ArgumentNullException(nameof(clrType));
            }

            Definition.RuntimeType = clrType;
            Definition.Name = context.Naming.GetTypeName(clrType, TypeKind.Object);
            Definition.Description = context.Naming.GetTypeDescription(clrType, TypeKind.Object);
        }

        protected ObjectTypeDescriptor(IDescriptorContext context)
            : base(context)
        {
            Definition.RuntimeType = typeof(object);
        }

        protected ObjectTypeDescriptor(
            IDescriptorContext context,
            ObjectTypeDefinition definition)
            : base(context)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        protected internal override ObjectTypeDefinition Definition { get; protected set; } = new();

        protected ICollection<ObjectFieldDescriptor> Fields { get; } =
            new List<ObjectFieldDescriptor>();

        protected ICollection<Type> ResolverTypes => _resolverTypes ??= new HashSet<Type>();

        protected override void OnCreateDefinition(
            ObjectTypeDefinition definition)
        {
            if (Definition.FieldBindingType is not null)
            {
                Context.TypeInspector.ApplyAttributes(
                    Context,
                    this,
                    Definition.FieldBindingType);
            }

            var fields = new Dictionary<NameString, ObjectFieldDefinition>();
            var handledMembers = new HashSet<MemberInfo>();

            FieldDescriptorUtilities.AddExplicitFields(
                Fields.Select(t => t.CreateDefinition()),
                f => f.Member,
                fields,
                handledMembers);

            OnCompleteFields(fields, handledMembers);

            Definition.Fields.AddRange(fields.Values);

            base.OnCreateDefinition(definition);
        }

        protected virtual void OnCompleteFields(
            IDictionary<NameString, ObjectFieldDefinition> fields,
            ISet<MemberInfo> handledMembers)
        {
            DiscoverResolvers(fields);
        }

        protected void DiscoverResolvers(
            IDictionary<NameString, ObjectFieldDefinition> fields)
        {
            var processed = new HashSet<string>();

            if (Definition.RuntimeType != typeof(object))
            {
                foreach (Type resolverType in Context.TypeInspector
                    .GetResolverTypes(Definition.RuntimeType))
                {
                    ResolverTypes.Add(resolverType);
                }
            }

            foreach (Type resolverType in ResolverTypes)
            {
                AddResolvers(
                    fields,
                    processed,
                    Definition.RuntimeType ?? typeof(object),
                    resolverType);
            }
        }

        private void AddResolvers(
            IDictionary<NameString, ObjectFieldDefinition> fields,
            ISet<string> processed,
            Type sourceType,
            Type resolverType)
        {
            foreach (MemberInfo member in Context.TypeInspector.GetMembers(resolverType))
            {
                if (IsResolverRelevant(sourceType, member))
                {
                    ObjectFieldDefinition fieldDefinition =
                        ObjectFieldDescriptor
                            .New(Context, member, sourceType, resolverType)
                            .CreateDefinition();

                    if (processed.Add(fieldDefinition.Name))
                    {
                        fields[fieldDefinition.Name] = fieldDefinition;
                    }
                }
            }
        }

        private static bool IsResolverRelevant(
            Type sourceType,
            MemberInfo resolver)
        {
            switch (resolver)
            {
                case PropertyInfo:
                    return true;

                case MethodInfo m:
                    ParameterInfo parent = m.GetParameters()
                        .FirstOrDefault(t => t.IsDefined(typeof(ParentAttribute)));
                    return parent is null || parent.ParameterType.IsAssignableFrom(sourceType);

                default:
                    return false;
            }
        }

        public IObjectTypeDescriptor SyntaxNode(
            ObjectTypeDefinitionNode objectTypeDefinition)
        {
            Definition.SyntaxNode = objectTypeDefinition;
            return this;
        }

        public IObjectTypeDescriptor Name(NameString value)
        {
            Definition.Name = value.EnsureNotEmpty(nameof(value));
            return this;
        }

        public IObjectTypeDescriptor Description(string value)
        {
            Definition.Description = value;
            return this;
        }

        public IObjectTypeDescriptor Interface<TInterface>()
            where TInterface : InterfaceType
        {
            if (typeof(TInterface) == typeof(InterfaceType))
            {
                throw new ArgumentException(
                    ObjectTypeDescriptor_InterfaceBaseClass);
            }

            Definition.Interfaces.Add(
                Context.TypeInspector.GetTypeRef(typeof(TInterface)));
            return this;
        }

        public IObjectTypeDescriptor Interface<TInterface>(
            TInterface type)
            where TInterface : InterfaceType
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            Definition.Interfaces.Add(new SchemaTypeReference(
                type));
            return this;
        }

        public IObjectTypeDescriptor Interface(
            NamedTypeNode namedType)
        {
            if (namedType is null)
            {
                throw new ArgumentNullException(nameof(namedType));
            }

            Definition.Interfaces.Add(TypeReference.Create(namedType, TypeContext.Output));
            return this;
        }

        public IObjectTypeDescriptor Implements<T>()
            where T : InterfaceType =>
            Interface<T>();

        public IObjectTypeDescriptor Implements<T>(T type)
            where T : InterfaceType =>
            Interface(type);

        public IObjectTypeDescriptor Implements(NamedTypeNode type) =>
            Interface(type);

        public IObjectTypeDescriptor Include<TResolver>()
        {
            if (typeof(IType).IsAssignableFrom(typeof(TResolver)))
            {
                throw new ArgumentException(ObjectTypeDescriptor_Resolver_SchemaType);
            }

            ResolverTypes.Add(typeof(TResolver));
            return this;
        }

        public IObjectTypeDescriptor IsOfType(IsOfType isOfType)
        {
            Definition.IsOfType = isOfType
                ?? throw new ArgumentNullException(nameof(isOfType));
            return this;
        }

        public IObjectFieldDescriptor Field(NameString name)
        {
            ObjectFieldDescriptor fieldDescriptor =
                Fields.FirstOrDefault(t => t.Definition.Name.Equals(name));
            if (fieldDescriptor is { })
            {
                return fieldDescriptor;
            }

            fieldDescriptor = ObjectFieldDescriptor.New(Context, name);
            Fields.Add(fieldDescriptor);
            return fieldDescriptor;
        }

        public IObjectFieldDescriptor Field<TResolver>(
            Expression<Func<TResolver, object>> propertyOrMethod) =>
            Field<TResolver, object>(propertyOrMethod);

        public IObjectFieldDescriptor Field(
            MemberInfo propertyOrMethod)
        {
            if (propertyOrMethod is null)
            {
                throw new ArgumentNullException(nameof(propertyOrMethod));
            }

            if (propertyOrMethod is PropertyInfo || propertyOrMethod is MethodInfo)
            {
                ObjectFieldDescriptor fieldDescriptor =
                    Fields.FirstOrDefault(t => t.Definition.Member == propertyOrMethod);
                if (fieldDescriptor is not null)
                {
                    return fieldDescriptor;
                }

                fieldDescriptor = ObjectFieldDescriptor.New(
                    Context,
                    propertyOrMethod,
                    Definition.RuntimeType,
                    propertyOrMethod.ReflectedType ?? Definition.RuntimeType);
                Fields.Add(fieldDescriptor);
                return fieldDescriptor;
            }

            throw new ArgumentException(
                ObjectTypeDescriptor_MustBePropertyOrMethod,
                nameof(propertyOrMethod));
        }

        public IObjectFieldDescriptor Field<TResolver, TPropertyType>(
            Expression<Func<TResolver, TPropertyType>> propertyOrMethod)
        {
            if (propertyOrMethod is null)
            {
                throw new ArgumentNullException(nameof(propertyOrMethod));
            }

            MemberInfo member = propertyOrMethod.TryExtractMember();

            if (member is PropertyInfo || member is MethodInfo)
            {
                ObjectFieldDescriptor fieldDescriptor =
                    Fields.FirstOrDefault(t => t.Definition.Member == member);
                if (fieldDescriptor is { })
                {
                    return fieldDescriptor;
                }

                fieldDescriptor = ObjectFieldDescriptor.New(
                    Context, member, Definition.RuntimeType, typeof(TResolver));
                Fields.Add(fieldDescriptor);
                return fieldDescriptor;
            }

            if (member is null)
            {
                var fieldDescriptor = ObjectFieldDescriptor.New(
                    Context, propertyOrMethod, Definition.RuntimeType, typeof(TResolver));
                Fields.Add(fieldDescriptor);
                return fieldDescriptor;
            }

            throw new ArgumentException(
                ObjectTypeDescriptor_MustBePropertyOrMethod,
                nameof(member));
        }

        public IObjectTypeDescriptor Directive<T>(T directiveInstance)
            where T : class
        {
            Definition.AddDirective(directiveInstance, Context.TypeInspector);
            return this;
        }

        public IObjectTypeDescriptor Directive<T>()
            where T : class, new()
        {
            Definition.AddDirective(new T(), Context.TypeInspector);
            return this;
        }

        public IObjectTypeDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            Definition.AddDirective(name, arguments);
            return this;
        }

        public static ObjectTypeDescriptor New(
            IDescriptorContext context) =>
            new(context);

        public static ObjectTypeDescriptor New(
            IDescriptorContext context,
            Type clrType) =>
            new(context, clrType);

        public static ObjectTypeDescriptor<T> New<T>(
            IDescriptorContext context) =>
            new(context);

        public static ObjectTypeExtensionDescriptor<T> NewExtension<T>(
            IDescriptorContext context) =>
            new(context);

        public static ObjectTypeDescriptor FromSchemaType(
            IDescriptorContext context,
            Type schemaType) =>
            new ObjectTypeDescriptor(context, schemaType)
            {
                Definition = { RuntimeType = typeof(object) }
            };

        public static ObjectTypeDescriptor From(
            IDescriptorContext context,
            ObjectTypeDefinition definition) =>
            new(context, definition);

        public static ObjectTypeDescriptor<T> From<T>(
            IDescriptorContext context,
            ObjectTypeDefinition definition) =>
            new(context, definition);
    }
}
