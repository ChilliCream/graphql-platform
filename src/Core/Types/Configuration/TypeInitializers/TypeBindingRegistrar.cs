
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Utilities;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    /// <summary>
    /// Registers and finalizes .net type to schema type bindings.
    /// </summary>
    internal class TypeBindingRegistrar
    {
        private readonly List<TypeBindingInfo> _typeBindings;

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
            foreach (KeyValuePair<InputObjectType, InputObjectTypeBinding> item
                in CreateInputObjectTypeBindings(typeRegistry))
            {
                typeRegistry.RegisterType(item.Key, item.Value);
            }
        }

        #region ObjectType Bindings

        private Dictionary<ObjectType, ObjectTypeBinding>
            CreateObjectTypeBindings(ITypeRegistry typeRegistry)
        {
            var typeBindings = new Dictionary<ObjectType, ObjectTypeBinding>();

            foreach (TypeBindingInfo typeBindingInfo in _typeBindings)
            {
                if (typeBindingInfo.Name == null)
                {
                    typeBindingInfo.Name =
                        typeBindingInfo.Type.GetGraphQLName();
                }

                IEnumerable<FieldBinding> fieldBindings = null;
                if (typeRegistry.TryGetType(
                    typeBindingInfo.Name, out ObjectType ot))
                {
                    fieldBindings =
                            CreateFieldBindings(typeBindingInfo, ot.Fields);
                    typeBindings[ot] = new ObjectTypeBinding(ot.Name,
                        typeBindingInfo.Type, ot, fieldBindings);
                }
            }

            return typeBindings;

        }

        private IEnumerable<FieldBinding> CreateFieldBindings(
            TypeBindingInfo typeBindingInfo,
            FieldCollection<ObjectField> fields)
        {
            var fieldBindings = new Dictionary<string, FieldBinding>();

            // create explicit field bindings
            foreach (FieldBindingInfo fieldBindingInfo in
                typeBindingInfo.Fields)
            {
                if (fieldBindingInfo.Name == null)
                {
                    fieldBindingInfo.Name =
                        fieldBindingInfo.Member.GetGraphQLName();
                }

                if (fields.TryGetField(
                        fieldBindingInfo.Name,
                        out ObjectField field))
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

                foreach (ObjectField field in fields
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

        private Dictionary<InputObjectType, InputObjectTypeBinding>
            CreateInputObjectTypeBindings(ITypeRegistry typeRegistry)
        {
            var typeBindings =
                new Dictionary<InputObjectType, InputObjectTypeBinding>();

            foreach (TypeBindingInfo typeBindingInfo in _typeBindings)
            {
                if (typeBindingInfo.Name == null)
                {
                    typeBindingInfo.Name =
                        typeBindingInfo.Type.GetGraphQLName();
                }

                IEnumerable<InputFieldBinding> fieldBindings = null;
                if (typeRegistry.TryGetType(typeBindingInfo.Name,
                    out InputObjectType iot))
                {
                    fieldBindings = CreateInputFieldBindings(
                        typeBindingInfo, iot.Fields);
                    typeBindings[iot] = new InputObjectTypeBinding(iot.Name,
                        typeBindingInfo.Type, iot, fieldBindings);
                }
            }

            return typeBindings;
        }

        private IEnumerable<InputFieldBinding> CreateInputFieldBindings(
            TypeBindingInfo typeBindingInfo,
            FieldCollection<InputField> fields)
        {
            var fieldBindings = new Dictionary<string, InputFieldBinding>();

            // create explicit field bindings
            foreach (FieldBindingInfo fieldBindingInfo in
                typeBindingInfo.Fields)
            {
                if (fieldBindingInfo.Name == null)
                {
                    fieldBindingInfo.Name =
                        fieldBindingInfo.Member.GetGraphQLName();
                }

                if (fields.TryGetField(
                    fieldBindingInfo.Name,
                    out InputField field)
                    && fieldBindingInfo.Member is PropertyInfo p)
                {
                    fieldBindings[field.Name] = new InputFieldBinding(
                        fieldBindingInfo.Name, p, field);
                }
            }

            // create implicit field bindings
            if (typeBindingInfo.Behavior == BindingBehavior.Implicit)
            {
                Dictionary<string, PropertyInfo> properties =
                    ReflectionUtils.GetProperties(typeBindingInfo.Type);
                foreach (InputField field in fields
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
