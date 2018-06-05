using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;

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

        public string Name { get; protected set; }
        public string Description { get; protected set; }
        public Type NativeType { get; protected set; }
        public bool IsIntrospection { get; protected set; }
        public IsOfType IsOfType { get; protected set; }
        public ImmutableList<FieldDescriptor> Fields { get; protected set; }
            = ImmutableList<FieldDescriptor>.Empty;
        public ImmutableList<TypeInfo> Interfaces { get; protected set; }
            = ImmutableList<TypeInfo>.Empty;

        #region IObjectTypeDescriptor<T>

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

            TypeInfo typeInfo = TypeInspector.Default.CreateTypeInfo(
                typeof(TInterface));
            Interfaces = Interfaces.Add(typeInfo);
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
        public ObjectTypeDescriptor(Type objectType)
            : base(objectType)
        {
            NativeType = typeof(T);
        }

        #region IObjectTypeDescriptor<T>

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
