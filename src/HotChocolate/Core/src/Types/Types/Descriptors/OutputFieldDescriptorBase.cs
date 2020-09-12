using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors.Definitions;

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
            var typeInfo = Context.TypeInspector.CreateTypeInfo(type);

            if (typeInfo.IsSchemaType && !typeInfo.IsOutputType())
            {
                throw new ArgumentException(
                    TypeResources.ObjectFieldDescriptorBase_FieldType);
            }

            Definition.SetMoreSpecificType(
                typeInfo.GetExtendedType(),
                TypeContext.Output);
        }

        protected void Type<TOutputType>(TOutputType outputType)
            where TOutputType : class, IOutputType
        {
            if (outputType is null)
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
            if (typeNode is null)
            {
                throw new ArgumentNullException(nameof(typeNode));
            }
            Definition.SetMoreSpecificType(typeNode, TypeContext.Output);
        }

        protected void Argument(
            NameString name,
            Action<IArgumentDescriptor> argument)
        {
            if (argument is null)
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
                AddDeprecatedDirective(reason);
            }
        }

        public void Deprecated()
        {
            Definition.DeprecationReason =
                WellKnownDirectives.DeprecationDefaultReason;
            AddDeprecatedDirective(null);
        }

        private void AddDeprecatedDirective(string reason)
        {
            if (_deprecatedDirective != null)
            {
                Definition.Directives.Remove(_deprecatedDirective);
            }

            _deprecatedDirective = new DirectiveDefinition(
                new DeprecatedDirective(reason),
                Context.TypeInspector.GetTypeRef(typeof(DeprecatedDirective)));
            Definition.Directives.Add(_deprecatedDirective);

            if (!_deprecatedDependencySet)
            {
                Definition.Dependencies.Add(new TypeDependency(
                    Context.TypeInspector.GetTypeRef(
                        typeof(DeprecatedDirectiveType)),
                    TypeDependencyKind.Completed));
                _deprecatedDependencySet = true;
            }
        }

        protected void Ignore(bool ignore = true)
        {
            Definition.Ignore = ignore;
        }

        protected void Directive<T>(T directive)
            where T : class
        {
            Definition.AddDirective(directive, Context.TypeInspector);
        }

        protected void Directive<T>()
            where T : class, new()
        {
            Definition.AddDirective(new T(), Context.TypeInspector);
        }

        protected void Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            Definition.AddDirective(name, arguments);
        }
    }
}
