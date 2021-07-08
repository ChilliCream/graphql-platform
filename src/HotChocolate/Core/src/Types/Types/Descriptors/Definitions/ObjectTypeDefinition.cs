using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions
{
    /// <summary>
    /// Defines the properties of a GraphQL object type.
    /// </summary>
    public class ObjectTypeDefinition
        : TypeDefinitionBase<ObjectTypeDefinitionNode>
        , IComplexOutputTypeDefinition
    {
        private List<Type>? _knownClrTypes;
        private List<ITypeReference>? _interfaces;
        private List<ObjectFieldBinding>? _fieldIgnores;

        /// <summary>
        /// Initializes a new instance of <see cref="ObjectTypeDefinition"/>.
        /// </summary>
        public ObjectTypeDefinition() { }

        /// <summary>
        /// Initializes a new instance of <see cref="ObjectTypeDefinition"/>.
        /// </summary>
        public ObjectTypeDefinition(
            NameString name,
            string? description = null,
            Type? runtimeType = null)
            : base(runtimeType ?? typeof(object))
        {
            Name = name;
            Description = description;
            FieldBindingType = runtimeType;
        }

        /// <summary>
        /// Gets or sets the .net type representation of this type.
        /// </summary>
        public override Type RuntimeType
        {
            get => base.RuntimeType;
            set
            {
                base.RuntimeType = value;
                FieldBindingType = value;
            }
        }

        /// <summary>
        /// The type that shall be used to infer fields from.
        /// </summary>
        public Type? FieldBindingType { get; set; }

        /// <summary>
        /// Runtime types that also represent this GraphQL type.
        /// </summary>
        public IList<Type> KnownRuntimeTypes =>
            _knownClrTypes ??= new List<Type>();

        /// <summary>
        /// Gets fields that shall be ignored.
        /// </summary>
        public IList<ObjectFieldBinding> FieldIgnores =>
            _fieldIgnores ??= new List<ObjectFieldBinding>();

        /// <summary>
        /// A delegate to determine if a resolver result is of this object type.
        /// </summary>
        public IsOfType? IsOfType { get; set; }

        /// <summary>
        /// Defines if this type definition represents a object type extension.
        /// </summary>
        public bool IsExtension { get; set; }

        /// <summary>
        /// Gets the interfaces that this object type implements.
        /// </summary>
        public IList<ITypeReference> Interfaces =>
            _interfaces ??= new List<ITypeReference>();

        /// <summary>
        /// Gets the fields of this object type.
        /// </summary>
        public IBindableList<ObjectFieldDefinition> Fields { get; } =
            new BindableList<ObjectFieldDefinition>();

        internal override IEnumerable<ILazyTypeConfiguration> GetConfigurations()
        {
            var configs = new List<ILazyTypeConfiguration>();

            configs.AddRange(Configurations);

            foreach (ObjectFieldDefinition field in Fields)
            {
                configs.AddRange(field.Configurations);

                foreach (ArgumentDefinition argument in field.GetArguments())
                {
                    configs.AddRange(argument.Configurations);
                }
            }

            return configs;
        }

        internal IReadOnlyList<Type> GetKnownClrTypes()
        {
            if (_knownClrTypes is null)
            {
                return Array.Empty<Type>();
            }

            return _knownClrTypes;
        }

        internal IReadOnlyList<ITypeReference> GetInterfaces()
        {
            if (_interfaces is null)
            {
                return Array.Empty<ITypeReference>();
            }

            return _interfaces;
        }

        internal IReadOnlyList<ObjectFieldBinding> GetFieldIgnores()
        {
            if (_fieldIgnores is null)
            {
                return Array.Empty<ObjectFieldBinding>();
            }

            return _fieldIgnores;
        }

        protected internal void CopyTo(ObjectTypeDefinition target)
        {
            base.CopyTo(target);

            if (_knownClrTypes is { Count: > 0 })
            {
                target._knownClrTypes = new List<Type>(_knownClrTypes);
            }

            if (_interfaces is { Count: > 0 })
            {
                target._interfaces = new List<ITypeReference>(_interfaces);
            }

            if (_fieldIgnores is { Count: > 0 })
            {
                target._fieldIgnores = new List<ObjectFieldBinding>(_fieldIgnores);
            }

            target.FieldBindingType = FieldBindingType;
            target.IsOfType = IsOfType;
            target.IsExtension = IsExtension;
        }

        protected internal void MergeInto(ObjectTypeDefinition target)
        {
            base.MergeInto(target);

            if (_knownClrTypes is { Count: > 0 })
            {
                target._knownClrTypes ??= new List<Type>();
                target._knownClrTypes.AddRange(_knownClrTypes);
            }

            if (_interfaces is { Count: > 0 })
            {
                target._interfaces ??= new List<ITypeReference>();
                target._interfaces.AddRange(_interfaces);
            }

            if (_fieldIgnores is { Count: > 0 })
            {
                target._fieldIgnores ??= new List<ObjectFieldBinding>();
                target._fieldIgnores.AddRange(_fieldIgnores);
            }

            foreach (var field in Fields)
            {
                ObjectFieldDefinition? targetField = field switch
                {
                    { BindTo: { Type: ObjectFieldBindingType.Property } bindTo } =>
                        target.Fields.FirstOrDefault(t => bindTo.Name.Equals(t.Member?.Name)),
                    { BindTo: { Type: ObjectFieldBindingType.Field } bindTo } =>
                        target.Fields.FirstOrDefault(t => bindTo.Name.Equals(t.Name)),
                    _ => target.Fields.FirstOrDefault(t => field.Name.Equals(t.Name))
                };

                var replaceField = field.BindTo?.Replace ?? false;
                var removeField = field.Ignore;

                // we skip fields that have an incompatible parent.
                if (field.Member is MethodInfo p &&
                    p.GetParameters() is { Length: > 0 } parameters)
                {
                    ParameterInfo? parent = parameters.FirstOrDefault(
                        t => t.IsDefined(typeof(ParentAttribute), true));
                    if (parent is not null &&
                        !parent.ParameterType.IsAssignableFrom(target.RuntimeType))
                    {
                        continue;
                    }
                }

                if (removeField)
                {
                    if (targetField is not null)
                    {
                        target.Fields.Remove(targetField);
                    }
                }
                else if (targetField is null || replaceField)
                {
                    if (targetField is not null)
                    {
                        target.Fields.Remove(targetField);
                    }

                    var newField = new ObjectFieldDefinition();
                    field.CopyTo(newField);
                    newField.SourceType = target.RuntimeType;

                    SetResolverMember(newField, targetField);

                    target.Fields.Add(newField);
                }
                else
                {
                    SetResolverMember(field, targetField);
                    field.MergeInto(targetField);
                }
            }

            target.IsOfType ??= IsOfType;
        }

        private static void SetResolverMember(
            ObjectFieldDefinition sourceField,
            ObjectFieldDefinition? targetField)
        {
            // we prepare the field that is merged in to use the resolver member instead of member.
            // this will ensure that the original source type member is preserved after we have
            // merged the type extensions.

            if (sourceField.Member is not null && sourceField.ResolverMember is null)
            {
                sourceField.ResolverMember = sourceField.Member;
                sourceField.Member = targetField?.Member;
            }
        }
    }
}
