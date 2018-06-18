
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    /// <summary>
    /// Registers and finalizes .net type to schema type bindings.
    /// </summary>
    internal class TypeBindingRegistrar
    {
        private List<TypeBindingInfo> _typeBindings;

        public TypeBindingRegistrar(IEnumerable<TypeBindingInfo> typeBindings)
        {
            if (typeBindings == null)
            {
                throw new ArgumentNullException(nameof(typeBindings));
            }

            _typeBindings = new List<TypeBindingInfo>(typeBindings);
        }

        public void RegisterTypeBindings(ITypeRegistry typeRegistry)
        {
            // bind object types
            foreach (KeyValuePair<ObjectType, ObjectTypeBinding> item in
                CreateObjectTypeBindings(typeRegistry))
            {
                typeRegistry.RegisterType(item.Key, item.Value);
            }

            // bind input object types
            foreach (KeyValuePair<InputObjectType, InputObjectTypeBinding> item in
                CreateInputObjectTypeBindings(typeRegistry))
            {
                typeRegistry.RegisterType(item.Key, item.Value);
            }
        }

        #region ObjectType Bindings

        private Dictionary<ObjectType, ObjectTypeBinding> CreateObjectTypeBindings(
            ITypeRegistry typeRegistry)
        {
            Dictionary<ObjectType, ObjectTypeBinding> typeBindings =
                new Dictionary<ObjectType, ObjectTypeBinding>();

            foreach (TypeBindingInfo typeBindingInfo in _typeBindings)
            {
                if (typeBindingInfo.Name == null)
                {
                    typeBindingInfo.Name = typeBindingInfo.Type.GetGraphQLName();
                }

                IEnumerable<FieldBinding> fieldBindings = null;
                if (typeRegistry.TryGetType<ObjectType>(
                    typeBindingInfo.Name, out ObjectType ot))
                {
                    fieldBindings = CreateFieldBindings(typeBindingInfo, ot.Fields);
                    typeBindings[ot] = new ObjectTypeBinding(ot.Name,
                        typeBindingInfo.Type, ot, fieldBindings);
                }
            }

            return typeBindings;
        }

        private IEnumerable<FieldBinding> CreateFieldBindings(
            TypeBindingInfo typeBindingInfo,
            IReadOnlyDictionary<string, Field> fields)
        {
            Dictionary<string, FieldBinding> fieldBindings =
                new Dictionary<string, FieldBinding>();

            // create explicit field bindings
            foreach (FieldBindingInfo fieldBindingInfo in
                typeBindingInfo.Fields)
            {
                if (fieldBindingInfo.Name == null)
                {
                    fieldBindingInfo.Name = fieldBindingInfo.Member.GetGraphQLName();
                }

                if (fields.TryGetValue(fieldBindingInfo.Name, out Field field))
                {
                    fieldBindings[field.Name] = new FieldBinding(
                        fieldBindingInfo.Name, fieldBindingInfo.Member, field);
                }
            }

            // create implicit field bindings
            if (typeBindingInfo.Behavior == BindingBehavior.Implicit)
            {
                Dictionary<string, MemberInfo> members =
                    ReflectionUtils.GetMembers(typeBindingInfo.Type);
                foreach (Field field in fields.Values
                    .Where(t => !fieldBindings.ContainsKey(t.Name)))
                {
                    if (members.TryGetValue(field.Name, out MemberInfo member))
                    {
                        fieldBindings[field.Name] = new FieldBinding(
                            field.Name, member, field);
                    }
                }
            }

            return fieldBindings.Values;
        }

        #endregion

        #region InputObjectType Bindings

        private Dictionary<InputObjectType, InputObjectTypeBinding> CreateInputObjectTypeBindings(
            ITypeRegistry typeRegistry)
        {
            Dictionary<InputObjectType, InputObjectTypeBinding> typeBindings =
                new Dictionary<InputObjectType, InputObjectTypeBinding>();

            foreach (TypeBindingInfo typeBindingInfo in _typeBindings)
            {
                if (typeBindingInfo.Name == null)
                {
                    typeBindingInfo.Name = typeBindingInfo.Type.GetGraphQLName();
                }

                IEnumerable<InputFieldBinding> fieldBindings = null;
                if (typeRegistry.TryGetType<InputObjectType>(
                    typeBindingInfo.Name, out InputObjectType iot))
                {
                    fieldBindings = CreateInputFieldBindings(typeBindingInfo, iot.Fields);
                    typeBindings[iot] = new InputObjectTypeBinding(iot.Name,
                        typeBindingInfo.Type, iot, fieldBindings);
                }
            }

            return typeBindings;
        }

        private IEnumerable<InputFieldBinding> CreateInputFieldBindings(
            TypeBindingInfo typeBindingInfo,
            IReadOnlyDictionary<string, InputField> fields)
        {
            Dictionary<string, InputFieldBinding> fieldBindings =
                new Dictionary<string, InputFieldBinding>();

            // create explicit field bindings
            foreach (FieldBindingInfo fieldBindingInfo in
                typeBindingInfo.Fields)
            {
                if (fieldBindingInfo.Name == null)
                {
                    fieldBindingInfo.Name = fieldBindingInfo.Member.GetGraphQLName();
                }

                if (fields.TryGetValue(fieldBindingInfo.Name, out InputField field))
                {
                    if (fieldBindingInfo.Member is PropertyInfo p)
                    {
                        fieldBindings[field.Name] = new InputFieldBinding(
                            fieldBindingInfo.Name, p, field);
                    }
                    // TODO : error -> exception?
                }
            }

            // create implicit field bindings
            if (typeBindingInfo.Behavior == BindingBehavior.Implicit)
            {
                Dictionary<string, PropertyInfo> properties =
                    ReflectionUtils.GetProperties(typeBindingInfo.Type);
                foreach (InputField field in fields.Values
                    .Where(t => !fieldBindings.ContainsKey(t.Name)))
                {
                    if (properties.TryGetValue(field.Name,
                        out PropertyInfo property))
                    {
                        fieldBindings[field.Name] = new InputFieldBinding(
                            field.Name, property, field);
                    }
                }
            }

            return fieldBindings.Values;
        }

        #endregion
    }
}
