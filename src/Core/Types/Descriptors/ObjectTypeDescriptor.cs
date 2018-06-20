using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class ObjectTypeDescriptor
        : IObjectTypeDescriptor
    {
        public ObjectTypeDescriptor(Type objectType)
        {
            if (objectType == null)
            {
                throw new ArgumentNullException(nameof(objectType));
            }

            Name = objectType.GetGraphQLName();
        }

        public ObjectTypeDescriptor(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(
                    "The name cannot be null or empty.",
                    nameof(name));
            }

            Name = name;
        }

        public ObjectTypeDefinitionNode SyntaxNode { get; protected set; }

        public string Name { get; protected set; }

        public string Description { get; protected set; }

        public Type NativeType { get; protected set; }

        public bool IsIntrospection { get; protected set; }

        public IsOfType IsOfType { get; protected set; }

        protected ImmutableList<FieldDescriptor> Fields { get; set; }
            = ImmutableList<FieldDescriptor>.Empty;

        public ImmutableList<TypeReference> Interfaces { get; protected set; }
            = ImmutableList<TypeReference>.Empty;

        public virtual IReadOnlyCollection<FieldDescriptor> GetFieldDescriptors()
        {
            Dictionary<string, FieldDescriptor> descriptors =
                new Dictionary<string, FieldDescriptor>();
            foreach (FieldDescriptor descriptor in Fields)
            {
                descriptors[descriptor.Name] = descriptor;
            }
            return descriptors.Values;
        }

        #region IObjectTypeDescriptor<T>

        IObjectTypeDescriptor IObjectTypeDescriptor.SyntaxNode(
            ObjectTypeDefinitionNode syntaxNode)
        {
            SyntaxNode = syntaxNode;
            return this;
        }

        IObjectTypeDescriptor IObjectTypeDescriptor.Name(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(
                    "The name cannot be null or empty.",
                    nameof(name));
            }

            if (!ValidationHelper.IsTypeNameValid(name))
            {
                throw new ArgumentException(
                    "The specified name is not a valid GraphQL type name.",
                    nameof(name));
            }

            Name = name;
            return this;
        }

        IObjectTypeDescriptor IObjectTypeDescriptor.Description(string description)
        {
            Description = description;
            return this;
        }

        IObjectTypeDescriptor IObjectTypeDescriptor.Interface<TInterface>()
        {
            if (typeof(TInterface) == typeof(InterfaceType))
            {
                throw new ArgumentException(
                    "The interface type has to be inherited.");
            }

            Interfaces = Interfaces.Add(new TypeReference(typeof(TInterface)));
            return this;
        }

        IObjectTypeDescriptor IObjectTypeDescriptor.Interface(NamedTypeNode type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            Interfaces = Interfaces.Add(new TypeReference(type));
            return this;
        }

        IObjectTypeDescriptor IObjectTypeDescriptor.IsOfType(IsOfType isOfType)
        {
            if (isOfType == null)
            {
                throw new ArgumentNullException(nameof(isOfType));
            }

            IsOfType = isOfType;
            return this;
        }

        IFieldDescriptor IObjectTypeDescriptor.Field(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(
                    "The field name cannot be null or empty.",
                    nameof(name));
            }

            if (!ValidationHelper.IsFieldNameValid(name))
            {
                throw new ArgumentException(
                    "The specified name is not a valid GraphQL field name.",
                    nameof(name));
            }

            FieldDescriptor fieldDescriptor = new FieldDescriptor(Name, name);
            Fields = Fields.Add(fieldDescriptor);
            return fieldDescriptor;
        }

        #endregion
    }

    internal class ObjectTypeDescriptor<T>
        : ObjectTypeDescriptor
        , IObjectTypeDescriptor<T>
    {
        private BindingBehavior _bindingBehavior = BindingBehavior.Implicit;

        public ObjectTypeDescriptor(Type pocoType)
            : base(pocoType)
        {
            NativeType = pocoType;
        }

        public override IReadOnlyCollection<FieldDescriptor> GetFieldDescriptors()
        {
            Dictionary<string, FieldDescriptor> descriptors =
                new Dictionary<string, FieldDescriptor>();

            foreach (FieldDescriptor descriptor in Fields)
            {
                descriptors[descriptor.Name] = descriptor;
            }

            if (_bindingBehavior == BindingBehavior.Implicit)
            {
                Dictionary<MemberInfo, string> members = GetMembers(NativeType);
                foreach (FieldDescriptor descriptor in descriptors.Values
                    .Where(t => t.Member != null))
                {
                    members.Remove(descriptor.Member);
                }

                foreach (KeyValuePair<MemberInfo, string> member in members)
                {
                    if (!descriptors.ContainsKey(member.Value))
                    {
                        Type returnType = member.Key.GetReturnType();
                        if (returnType != null)
                        {
                            descriptors[member.Value] =
                                new FieldDescriptor(Name, member.Key, returnType);
                        }
                    }
                }
            }

            return descriptors.Values;
        }

        private static Dictionary<MemberInfo, string> GetMembers(Type type)
        {
            Dictionary<MemberInfo, string> members =
                new Dictionary<MemberInfo, string>();

            foreach (PropertyInfo property in type.GetProperties(
                BindingFlags.Instance | BindingFlags.Public)
                .Where(t => t.DeclaringType != typeof(object)))
            {
                members[property] = property.GetGraphQLName();
            }

            foreach (MethodInfo method in type.GetMethods(
                BindingFlags.Instance | BindingFlags.Public)
                .Where(m => !m.IsSpecialName && m.DeclaringType != typeof(object)))
            {
                members[method] = method.GetGraphQLName();
            }

            return members;
        }

        #region IObjectTypeDescriptor<T>

        IObjectTypeDescriptor<T> IObjectTypeDescriptor<T>.Name(string name)
        {
            ((IObjectTypeDescriptor)this).Name(name);
            return this;
        }

        IObjectTypeDescriptor<T> IObjectTypeDescriptor<T>.Description(string description)
        {
            ((IObjectTypeDescriptor)this).Description(description);
            return this;
        }

        IObjectTypeDescriptor<T> IObjectTypeDescriptor<T>.BindFields(BindingBehavior bindingBehavior)
        {
            _bindingBehavior = bindingBehavior;
            return this;
        }

        IObjectTypeDescriptor<T> IObjectTypeDescriptor<T>.Interface<TInterface>()
        {
            ((IObjectTypeDescriptor)this).Interface<TInterface>();
            return this;
        }

        IObjectTypeDescriptor<T> IObjectTypeDescriptor<T>.IsOfType(IsOfType isOfType)
        {
            ((IObjectTypeDescriptor)this).IsOfType(isOfType);
            return this;
        }

        IFieldDescriptor IObjectTypeDescriptor<T>.Field<TValue>(Expression<Func<T, TValue>> methodOrProperty)
        {
            if (methodOrProperty == null)
            {
                throw new ArgumentNullException(nameof(methodOrProperty));
            }

            MemberInfo member = methodOrProperty.ExtractMember();
            if (member is PropertyInfo || member is MethodInfo)
            {
                FieldDescriptor fieldDescriptor = new FieldDescriptor(
                    Name, member, typeof(TValue));
                Fields = Fields.Add(fieldDescriptor);
                return fieldDescriptor;
            }

            throw new ArgumentException(
                "A field of an entity can only be a property or a method.",
                nameof(member));
        }

        #endregion
    }
}
