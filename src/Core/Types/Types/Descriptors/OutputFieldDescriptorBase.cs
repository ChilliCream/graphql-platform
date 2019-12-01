using System.Collections.Generic;
using System;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors.Definitions;
using System.Reflection;
using System.Linq;

namespace HotChocolate.Types.Descriptors
{
    public abstract class OutputFieldDescriptorBase<TDefinition>
        : DescriptorBase<TDefinition>
        where TDefinition : OutputFieldDefinitionBase
    {
        private bool _deprecatedDependencySet;
        private DirectiveDefinition _deprecatedDirective;

        protected OutputFieldDescriptorBase(IDescriptorContext context)
            : base(context)
        {
        }

        protected ICollection<ArgumentDescriptor> Arguments { get; } =
            new List<ArgumentDescriptor>();

        protected IReadOnlyDictionary<NameString, ParameterInfo> Parameters { get; set; }

        protected override void OnCreateDefinition(TDefinition definition)
        {
            base.OnCreateDefinition(definition);

            foreach (ArgumentDescriptor argument in Arguments)
            {
                Definition.Arguments.Add(argument.CreateDefinition());
            }
        }

        protected void SyntaxNode(
            FieldDefinitionNode syntaxNode)
        {
            Definition.SyntaxNode = syntaxNode;
        }

        protected void Name(NameString name)
        {
            Definition.Name = name.EnsureNotEmpty(nameof(name));
        }

        protected void Description(string description)
        {
            Definition.Description = description;
        }

        protected void Type<TOutputType>()
            where TOutputType : IOutputType
        {
            Type(typeof(TOutputType));
        }

        protected void Type(Type type)
        {
            Type extractedType = Context.Inspector.ExtractType(type);

            if (Context.Inspector.IsSchemaType(extractedType)
                && !typeof(IOutputType).IsAssignableFrom(extractedType))
            {
                throw new ArgumentException(
                    TypeResources.ObjectFieldDescriptorBase_FieldType);
            }

            Definition.SetMoreSpecificType(
                type,
                TypeContext.Output);
        }

        protected void Type<TOutputType>(TOutputType outputType)
            where TOutputType : class, IOutputType
        {
            if (outputType == null)
            {
                throw new ArgumentNullException(nameof(outputType));
            }

            if (!outputType.IsOutputType())
            {
                throw new ArgumentException(
                    TypeResources.ObjectFieldDescriptorBase_FieldType);
            }

            Definition.Type = new SchemaTypeReference(outputType);
        }

        protected void Type(ITypeNode typeNode)
        {
            if (typeNode == null)
            {
                throw new ArgumentNullException(nameof(typeNode));
            }
            Definition.SetMoreSpecificType(typeNode, TypeContext.Output);
        }

        protected void Argument(
            NameString name,
            Action<IArgumentDescriptor> argument)
        {
            if (argument == null)
            {
                throw new ArgumentNullException(nameof(argument));
            }

            name.EnsureNotEmpty(nameof(name));

            ParameterInfo parameter = null;
            Parameters?.TryGetValue(name, out parameter);

            ArgumentDescriptor descriptor = parameter is null
                ? Arguments.FirstOrDefault(t => t.Definition.Name.Equals(name))
                : Arguments.FirstOrDefault(t => t.Definition.Parameter == parameter);

            if (descriptor is null)
            {
                descriptor = parameter is null
                    ? ArgumentDescriptor.New(Context, name)
                    : ArgumentDescriptor.New(Context, parameter);
                Arguments.Add(descriptor);
            }

            argument(descriptor);
        }

        public void Deprecated(string reason)
        {
            if (string.IsNullOrEmpty(reason))
            {
                Deprecated();
            }
            else
            {
                Definition.DeprecationReason = reason;
                AddDeprectedDirective(reason);
            }
        }

        public void Deprecated()
        {
            Definition.DeprecationReason =
                WellKnownDirectives.DeprecationDefaultReason;
            AddDeprectedDirective(null);
        }

        private void AddDeprectedDirective(string reason)
        {
            if (_deprecatedDirective != null)
            {
                Definition.Directives.Remove(_deprecatedDirective);
            }

            _deprecatedDirective = new DirectiveDefinition(
                new DeprecatedDirective(reason));
            Definition.Directives.Add(_deprecatedDirective);

            if (!_deprecatedDependencySet)
            {
                Definition.Dependencies.Add(new TypeDependency(
                    new ClrTypeReference(
                        typeof(DeprecatedDirectiveType),
                        TypeContext.None),
                    TypeDependencyKind.Completed));
                _deprecatedDependencySet = true;
            }
        }

        protected void Ignore()
        {
            Definition.Ignore = true;
        }

        protected void Directive<T>(T directive)
            where T : class
        {
            Definition.AddDirective(directive);
        }

        protected void Directive<T>()
            where T : class, new()
        {
            Definition.AddDirective(new T());
        }

        protected void Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            Definition.AddDirective(name, arguments);
        }
    }
}
