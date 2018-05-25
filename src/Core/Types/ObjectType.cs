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
        , ITypeInitializer
        , IHasFields
    {

        private readonly IsOfType _isOfType;
        private readonly Func<SchemaContext, IEnumerable<InterfaceType>> _interfaceFactory;
        private Dictionary<string, InterfaceType> _interfaceMap =
            new Dictionary<string, InterfaceType>();
        private string _name;
        private string _description;
        private readonly Dictionary<string, Field> _fieldMap =
            new Dictionary<string, Field>();

        public ObjectType() { }

        internal ObjectType(ObjectTypeConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (string.IsNullOrEmpty(config.Name))
            {
                throw new ArgumentException(
                    "A type name must not be null or empty.",
                    nameof(config));
            }

            Field[] fields = config.Fields?.ToArray()
                 ?? Array.Empty<Field>();
            if (fields.Length == 0)
            {
                throw new ArgumentException(
                    $"The interface type `{Name}` has no fields.",
                    nameof(config));
            }

            foreach (Field field in fields)
            {
                if (_fieldMap.ContainsKey(field.Name))
                {
                    throw new ArgumentException(
                        $"The field name `{field.Name}` " +
                        $"is not unique within `{Name}`.",
                        nameof(config));
                }
                else
                {
                    _fieldMap.Add(field.Name, field);
                }
            }

            _isOfType = config.IsOfType;
            _interfaceFactory = s => config.Interfaces();

            SyntaxNode = config.SyntaxNode;
            Name = config.Name;
            Description = config.Description;
            IsIntrospection = config.IsIntrospection;
        }

        public ObjectTypeDefinitionNode SyntaxNode { get; }

        public string Name
        {
            get => _name;
            private set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException(nameof(value));
                }

                if (!string.IsNullOrEmpty(_name))
                {
                    throw new InvalidOperationException(
                        "The name property can only be set once.");
                }

                _name = value;
            }
        }

        public string Description
        {
            get => _description;
            private set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException(nameof(value));
                }

                if (!string.IsNullOrEmpty(_description))
                {
                    throw new InvalidOperationException(
                        "The description property can only be set once.");
                }

                _description = value;
            }
        }

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

        void ITypeInitializer.CompleteInitialization(
            SchemaContext schemaContext,
            Action<SchemaError> reportError)
        {
            foreach (Field field in _fieldMap.Values)
            {
                field.CompleteInitialization(reportError, this);
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
