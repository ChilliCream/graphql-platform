using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    public class Field
        : ITypeSystemNode
    {
        private readonly Func<SchemaContext, IOutputType> _typeFactory;
        private readonly Func<SchemaContext, FieldResolverDelegate> _resolverFactory;
        private readonly Dictionary<string, InputField> _argumentMap =
            new Dictionary<string, InputField>();
        private IOutputType _type;
        private FieldResolverDelegate _resolver;

        internal Field(FieldConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (string.IsNullOrEmpty(config.Name))
            {
                throw new ArgumentException(
                    "A field name must not be null or empty.",
                    nameof(config));
            }

            if (config.Type == null)
            {
                throw new ArgumentException(
                    "A field type must not be null or empty.",
                    nameof(config));
            }

            if (config.Arguments != null)
            {
                foreach (InputField argument in config.Arguments)
                {
                    if (_argumentMap.ContainsKey(argument.Name))
                    {
                        throw new ArgumentException(
                            $"The argument names are not unique -> argument: `{argument.Name}`.",
                            nameof(config));
                    }
                    else
                    {
                        _argumentMap.Add(argument.Name, argument);
                    }
                }
            }

            _typeFactory = config.Type;
            _resolverFactory = config.Resolver;

            SyntaxNode = config.SyntaxNode;
            Name = config.Name;
            Description = config.Description;
            IsIntrospection = config.IsIntrospection;
            IsDeprecated = !string.IsNullOrEmpty(config.DeprecationReason);
            DeprecationReason = config.DeprecationReason;
        }

        public FieldDefinitionNode SyntaxNode { get; }

        public string Name { get; }

        public string Description { get; }

        internal bool IsIntrospection { get; }

        public bool IsDeprecated { get; }

        public string DeprecationReason { get; }

        public IOutputType Type => _type;

        public IReadOnlyDictionary<string, InputField> Arguments => _argumentMap;

        public FieldResolverDelegate Resolver => _resolver;

        #region TypeSystemNode

        ISyntaxNode IHasSyntaxNode.SyntaxNode => SyntaxNode;
        IEnumerable<ITypeSystemNode> ITypeSystemNode.GetNodes()
            => _argumentMap.Values;

        #endregion

        #region Initialization

        internal void CompleteInitialization(
            SchemaContext schemaContext,
            Action<SchemaError> reportError,
            INamedType parentType)
        {
            _type = _typeFactory(schemaContext);
            if (_type == null)
            {
                reportError(new SchemaError(
                    $"The type of field `{Name}` is null.",
                    parentType));
            }

            foreach (InputField argument in _argumentMap.Values)
            {
                argument.CompleteInitialization(reportError, parentType);
            }


            if (parentType is ObjectType)
            {
                if (_resolverFactory == null)
                {
                    reportError(new SchemaError(
                        $"The field `{Name}` of object type `{parentType.Name}` " +
                        "has no resolver factory.", parentType));
                }
                else
                {
                    _resolver = _resolverFactory(schemaContext);
                    if (_resolver == null)
                    {
                        reportError(new SchemaError(
                            $"The field `{Name}` of object type `{parentType.Name}` " +
                            "has no resolver.", parentType));
                    }
                }
            }
        }

        #endregion
    }

    internal static class ValidationHelper
    {
        public static bool IsTypeNameValid(string typeName)
        {
            return true;
        }

        public static bool IsFieldNameValid(string typeName)
        {
            return true;
        }
    }

    internal class ObjectTypeDescriptor<T>
        : IObjectTypeDescriptor<T>
    {
        private readonly List<IFieldDescriptor> _fields = new List<IFieldDescriptor>();
        private readonly List<Type> _interfaces = new List<Type>();

        public string Name { get; private set; }
        public string Description { get; private set; }
        public IReadOnlyCollection<IFieldDescriptor> Fields => _fields;

        #region IObjectTypeDescriptor<T>

        IObjectTypeDescriptor IObjectTypeDescriptor.Name(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(
                    "The name cannot be null or empty.",
                    nameof(name));
            }

            if (ValidationHelper.IsTypeNameValid(name))
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
            return this;
        }

        IObjectTypeDescriptor IObjectTypeDescriptor.IsOfType(IsOfType isOfType)
        {
            return this;
        }

        IFieldDescriptor IObjectTypeDescriptor<T>.Field<TValue>(Expression<Func<T, TValue>> property)
        {
            throw new NotImplementedException();
        }

        IFieldDescriptor IObjectTypeDescriptor.Field(string name)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    internal class FieldDescriptor
        : IFieldDescriptor
    {
        public FieldDescriptor(string name)
        {

        }

        public FieldDescriptor(PropertyInfo property)
        {

        }

        public IFieldDescriptor Argument(string name, Action<IArgumentDescriptor> argument)
        {
            throw new NotImplementedException();
        }

        public IFieldDescriptor DeprecationReason(string deprecationReason)
        {
            throw new NotImplementedException();
        }

        public IFieldDescriptor Description(string description)
        {
            throw new NotImplementedException();
        }

        public IFieldDescriptor Resolver(FieldResolverDelegate fieldResolver)
        {
            throw new NotImplementedException();
        }

        public IFieldDescriptor Type<IOutputType>()
        {
            throw new NotImplementedException();
        }
    }


    public interface IObjectTypeDescriptor<T>
        : IObjectTypeDescriptor
    {
        IFieldDescriptor Field<TValue>(Expression<Func<T, TValue>> property);
    }

    public interface IObjectTypeDescriptor
    {
        IObjectTypeDescriptor Name(string name);
        IObjectTypeDescriptor Description(string description);
        IObjectTypeDescriptor Interface<T>()
            where T : InterfaceType;
        IObjectTypeDescriptor IsOfType(IsOfType isOfType);
        IFieldDescriptor Field(string name);
    }

    public interface IFieldDescriptor
    {
        IFieldDescriptor Description(string description);
        IFieldDescriptor DeprecationReason(string deprecationReason);
        IFieldDescriptor Type<IOutputType>();
        IFieldDescriptor Argument(string name, Action<IArgumentDescriptor> argument);
        IFieldDescriptor Resolver(FieldResolverDelegate fieldResolver);
    }

    public interface IArgumentDescriptor
    {
        IArgumentDescriptor Description(string description);
        IArgumentDescriptor Type<IInputType>();
        IArgumentDescriptor DefaultValue(IValueNode valueNode);
    }
}
