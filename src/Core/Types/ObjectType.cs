using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    public class ObjectType
        : INamedType
        , IOutputType
        , INullableType
        , ITypeSystemNode
        , INeedsInitialization
        , IHasFields
    {
        private readonly IsOfType _isOfType;
        private readonly Func<ITypeRegistry, IEnumerable<InterfaceType>> _interfaceFactory;
        private readonly IReadOnlyCollection<TypeInfo> _interfaceTypeInfos;
        private readonly ObjectTypeBinding _typeBinding;
        private Dictionary<string, InterfaceType> _interfaceMap =
            new Dictionary<string, InterfaceType>();
        private readonly Dictionary<string, Field> _fieldMap =
            new Dictionary<string, Field>();

        public ObjectType()
        {
            ObjectTypeDescriptor descriptor = CreateDescriptor();
            Configure(descriptor);

            if (string.IsNullOrEmpty(descriptor.Name))
            {
                throw new ArgumentException(
                    "The type name must not be null or empty.");
            }

            if (descriptor.Fields.Count == 0)
            {
                throw new ArgumentException(
                    $"The object type `{Name}` has no fields.");
            }

            List<FieldBinding> fieldBindings = new List<FieldBinding>();
            foreach (FieldDescriptor fieldDescriptor in descriptor.Fields)
            {
                Field field = fieldDescriptor.CreateField();
                _fieldMap[fieldDescriptor.Name] = field;

                if (fieldDescriptor.Member != null)
                {
                    fieldBindings.Add(new FieldBinding(
                        fieldDescriptor.Name, fieldDescriptor.Member, field));
                }
            }

            if (descriptor.NativeType != null)
            {
                _typeBinding = new ObjectTypeBinding(
                    descriptor.Name, descriptor.NativeType, this, fieldBindings);
            }

            _isOfType = descriptor.IsOfType;
            _interfaceFactory = r => descriptor.Interfaces
                .Select(t => t.TypeFactory(r))
                .Cast<InterfaceType>();
            _interfaceTypeInfos = descriptor.Interfaces;

            Name = descriptor.Name;
            Description = descriptor.Description;
            IsIntrospection = descriptor.IsIntrospection;
        }

        internal ObjectType(ObjectTypeConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (string.IsNullOrEmpty(config.Name))
            {
                throw new ArgumentException(
                    "An object type name must not be null or empty.",
                    nameof(config));
            }

            Field[] fields = config.Fields?.ToArray();
            if (fields == null || fields.Length == 0)
            {
                throw new ArgumentException(
                    $"The object type `{Name}` has no fields.",
                    nameof(config));
            }

            foreach (Field field in fields)
            {
                _fieldMap[field.Name] = field;
            }

            _isOfType = config.IsOfType;
            _interfaceFactory = config.Interfaces;

            SyntaxNode = config.SyntaxNode;
            Name = config.Name;
            Description = config.Description;
            IsIntrospection = config.IsIntrospection;
        }

        public ObjectTypeDefinitionNode SyntaxNode { get; }

        public string Name { get; }

        public string Description { get; }

        internal bool IsIntrospection { get; }

        public IReadOnlyDictionary<string, InterfaceType> Interfaces => _interfaceMap;

        public IReadOnlyDictionary<string, Field> Fields => _fieldMap;

        public bool IsOfType(IResolverContext context, object resolverResult)
            => _isOfType(context, resolverResult);

        #region Configuration

        internal virtual ObjectTypeDescriptor CreateDescriptor() =>
            new ObjectTypeDescriptor(GetType());

        protected virtual void Configure(IObjectTypeDescriptor descriptor) { }

        #endregion

        #region ITypeSystemNode

        ISyntaxNode IHasSyntaxNode.SyntaxNode => SyntaxNode;

        IEnumerable<ITypeSystemNode> ITypeSystemNode.GetNodes()
        {
            foreach (InterfaceType node in Interfaces.Values)
            {
                yield return node;
            }

            foreach (Field node in Fields.Values)
            {
                yield return node;
            }
        }

        #endregion

        #region Initialization

        void INeedsInitialization.RegisterDependencies(
            ISchemaContext schemaContext,
            Action<SchemaError> reportError)
        {
            if (_interfaceTypeInfos != null)
            {
                foreach (TypeInfo interfaceTypeInfo in _interfaceTypeInfos)
                {
                    schemaContext.Types.RegisterType(interfaceTypeInfo.NativeNamedType);
                }
            }

            foreach (Field field in _fieldMap.Values)
            {
                field.RegisterDependencies(schemaContext, reportError, this);
            }

            if (_typeBinding != null)
            {
                schemaContext.Types.RegisterType(this, _typeBinding);
            }
        }

        void INeedsInitialization.CompleteType(
            ISchemaContext schemaContext,
            Action<SchemaError> reportError)
        {
            foreach (Field field in _fieldMap.Values)
            {
                field.CompleteField(schemaContext, reportError, this);
            }

            if (_interfaceFactory != null)
            {
                foreach (InterfaceType interfaceType in
                    _interfaceFactory(schemaContext.Types))
                {
                    _interfaceMap[interfaceType.Name] = interfaceType;
                }

                CheckIfAllInterfaceFieldsAreImplemented(reportError);
            }
        }

        private void CheckIfAllInterfaceFieldsAreImplemented(
            Action<SchemaError> reportError)
        {
            foreach (InterfaceType interfaceType in _interfaceMap.Values)
            {
                foreach (Field interfaceField in interfaceType.Fields.Values)
                {
                    if (Fields.TryGetValue(interfaceField.Name, out Field field))
                    {
                        foreach (InputField interfaceArgument in interfaceField.Arguments.Values)
                        {
                            if (!field.Arguments.ContainsKey(
                                interfaceArgument.Name))
                            {
                                reportError(new SchemaError(
                                    $"Object type {Name} does not implement the " +
                                    $"field all arguments of field {interfaceField.Name} " +
                                    $"from interface {interfaceType.Name}.",
                                    this));
                            }
                        }
                    }
                    else
                    {
                        reportError(new SchemaError(
                            $"Object type {Name} does not implement the " +
                            $"field {interfaceField.Name} " +
                            $"from interface {interfaceType.Name}.",
                            this));
                    }
                }
            }
        }

        #endregion
    }

    public abstract class ObjectType<T>
        : ObjectType
    {
        public ObjectType()
        {
        }

        #region Configuration

        internal sealed override ObjectTypeDescriptor CreateDescriptor() =>
            new ObjectTypeDescriptor<T>(typeof(T));

        protected sealed override void Configure(IObjectTypeDescriptor descriptor)
        {
            Configure((IObjectTypeDescriptor<T>)descriptor);
        }

        protected abstract void Configure(IObjectTypeDescriptor<T> descriptor);

        #endregion
    }
}
