using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly ObjectTypeDescriptor _descriptor;
        private readonly IsOfType _isOfType;
        private readonly Func<SchemaContext, IEnumerable<InterfaceType>> _interfaceFactory;
        private Dictionary<string, InterfaceType> _interfaceMap =
            new Dictionary<string, InterfaceType>();
        private readonly Dictionary<string, Field> _fieldMap =
            new Dictionary<string, Field>();

        public ObjectType()
        {
            _descriptor = new ObjectTypeDescriptor();
            Configure(_descriptor);

            if (string.IsNullOrEmpty(_descriptor.Name))
            {
                throw new ArgumentException(
                    "A type name must not be null or empty.");
            }

            if (_descriptor.Fields.Count == 0)
            {
                throw new ArgumentException(
                    $"The object type `{Name}` has no fields.");
            }

            foreach (Field field in _descriptor.Fields.Select(t => t.CreateField()))
            {
                _fieldMap[field.Name] = field;
            }

            _isOfType = _descriptor.IsOfType;
            _interfaceFactory = s => _descriptor.Interfaces
                .Select(t => s.GetOrCreateType<InterfaceType>(t));

            Name = _descriptor.Name;
            Description = _descriptor.Description;
            IsIntrospection = _descriptor.IsIntrospection;
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
            _interfaceFactory = s => config.Interfaces();

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

        void INeedsInitialization.CompleteInitialization(
            SchemaContext schemaContext,
            Action<SchemaError> reportError)
        {
            foreach (Field field in _fieldMap.Values)
            {
                field.CompleteInitialization(schemaContext, reportError, this);
            }

            InterfaceType[] interfaces = _interfaceFactory?.Invoke(schemaContext)?.ToArray()
                ?? Array.Empty<InterfaceType>();

            foreach (InterfaceType interfaceType in interfaces)
            {
                if (_interfaceMap.TryGetValue(
                    interfaceType.Name, out InterfaceType type)
                    && interfaceType != type)
                {
                    reportError(new SchemaError(
                        "The interfaces that this object type implements " +
                        "are not unique.",
                        this));
                }
                else
                {
                    _interfaceMap[interfaceType.Name] = interfaceType;
                }
            }

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

        void INeedsInitialization.RegisterDependencies(
            ISchemaContextR schemaContext,
            Action<SchemaError> reportError)
        {



        }

        void INeedsInitialization.CompleteType(
            ISchemaContextR schemaContext,
            Action<SchemaError> reportError)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    public abstract class ObjectType<T>
        : ObjectType
    {
        public ObjectType()
        {
            Configure(default(IObjectTypeDescriptor<T>));
        }

        #region Configuration

        protected abstract void Configure(IObjectTypeDescriptor<T> descriptor);

        protected sealed override void Configure(IObjectTypeDescriptor descriptor) { }

        #endregion
    }
}
