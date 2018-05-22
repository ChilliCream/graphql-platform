using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly Func<IEnumerable<InterfaceType>> _interfaceFactory;
        private Dictionary<string, InterfaceType> _interfaceMap =
            new Dictionary<string, InterfaceType>();
        private readonly IEnumerable<Field> _fields;
        private readonly Dictionary<string, Field> _fieldMap =
            new Dictionary<string, Field>();

        public ObjectType(ObjectTypeConfig config)
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


            _isOfType = config.IsOfType;
            _interfaceFactory = config.Interfaces;
            _fields = config.Fields;

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

        // TODO : split method into InitFields, InitInterfaces, ValidateImplementation
        void ITypeInitializer.CompleteInitialization(Action<SchemaError> reportError)
        {
            Field[] fields = _fields.ToArray();
            if (fields.Length == 0)
            {
                reportError(new SchemaError(
                    $"The interface type {Name} has no fields.",
                    this));
            }

            foreach (Field field in fields)
            {
                field.CompleteInitialization(reportError, this);
                if (_fieldMap.ContainsKey(field.Name))
                {
                    reportError(new SchemaError(
                        $"The field name of field {field.Name} " +
                        $"is not unique within {Name}.",
                        this));
                }
                else
                {
                    _fieldMap.Add(field.Name, field);
                }
            }

            InterfaceType[] interfaces = _interfaceFactory?.Invoke()?.ToArray()
                ?? Array.Empty<InterfaceType>();
            foreach (InterfaceType interfaceType in interfaces)
            {
                if (_interfaceMap.TryGetValue(
                    interfaceType.Name, out InterfaceType type)
                    && interfaceType != type)
                {
                    reportError(new SchemaError(
                        "The interfaces that this object type implements are not unique.",
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
                            if (field.Arguments.TryGetValue(
                                interfaceArgument.Name, out InputField argument))
                            {
                                // TODO : implement equals and compare
                            }
                            else
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

    public class ObjectTypeConfig
        : INamedTypeConfig
    {
        public ObjectTypeDefinitionNode SyntaxNode { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        internal bool IsIntrospection { get; set; }

        public Func<IEnumerable<InterfaceType>> Interfaces { get; set; }

        public IEnumerable<Field> Fields { get; set; }

        public IsOfType IsOfType { get; set; }
    }
}
