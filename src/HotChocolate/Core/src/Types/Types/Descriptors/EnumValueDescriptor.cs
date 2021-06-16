using System;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Descriptors
{
    public class EnumValueDescriptor
        : DescriptorBase<EnumValueDefinition>
        , IEnumValueDescriptor
    {
        private bool _deprecatedDependencySet;
        private DirectiveDefinition _deprecatedDirective;

        protected EnumValueDescriptor(IDescriptorContext context, object runtimeValue)
            : base(context)
        {
            if (runtimeValue is null)
            {
                throw new ArgumentNullException(nameof(runtimeValue));
            }

            Definition.Name = context.Naming.GetEnumValueName(runtimeValue);
            Definition.RuntimeValue = runtimeValue;
            Definition.Description = context.Naming.GetEnumValueDescription(runtimeValue);
            Definition.Member = context.TypeInspector.GetEnumValueMember(runtimeValue);

            if (context.Naming.IsDeprecated(runtimeValue, out var reason))
            {
                Deprecated(reason);
            }
        }

        protected EnumValueDescriptor(IDescriptorContext context, EnumValueDefinition definition)
            : base(context)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        protected internal override EnumValueDefinition Definition { get; protected set; } =
            new EnumValueDefinition();

        protected override void OnCreateDefinition(EnumValueDefinition definition)
        {
            if (!Definition.AttributesAreApplied && Definition.Member is not null)
            {
                Context.TypeInspector.ApplyAttributes(
                    Context,
                    this,
                    Definition.Member);
                Definition.AttributesAreApplied = true;
            }

            base.OnCreateDefinition(definition);
        }

        public IEnumValueDescriptor SyntaxNode(
            EnumValueDefinitionNode enumValueDefinition)
        {
            Definition.SyntaxNode = enumValueDefinition;
            return this;
        }

        public IEnumValueDescriptor Name(NameString value)
        {
            Definition.Name = value.EnsureNotEmpty(nameof(value));
            return this;
        }

        public IEnumValueDescriptor Description(string value)
        {
            Definition.Description = value;
            return this;
        }

        [Obsolete("Use `Deprecated`.")]
        public IEnumValueDescriptor DeprecationReason(string reason) =>
            Deprecated(reason);

        public IEnumValueDescriptor Deprecated(string reason)
        {
            if (string.IsNullOrEmpty(reason))
            {
                return Deprecated();
            }
            else
            {
                Definition.DeprecationReason = reason;
                AddDeprecatedDirective(reason);
                return this;
            }
        }

        public IEnumValueDescriptor Deprecated()
        {
            Definition.DeprecationReason =
                WellKnownDirectives.DeprecationDefaultReason;
            AddDeprecatedDirective(null);
            return this;
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
                    Context.TypeInspector.GetTypeRef(typeof(DeprecatedDirectiveType)),
                    TypeDependencyKind.Completed));
                _deprecatedDependencySet = true;
            }
        }

        public IEnumValueDescriptor Directive<T>(T directiveInstance)
            where T : class
        {
            Definition.AddDirective(directiveInstance, Context.TypeInspector);
            return this;
        }

        public IEnumValueDescriptor Directive<T>()
            where T : class, new()
        {
            Definition.AddDirective(new T(), Context.TypeInspector);
            return this;
        }

        public IEnumValueDescriptor Directive(
            NameString name, params ArgumentNode[] arguments)
        {
            Definition.AddDirective(name, arguments);
            return this;
        }

        public static EnumValueDescriptor New(
            IDescriptorContext context,
            object value) =>
            new EnumValueDescriptor(context, value);

        public static EnumValueDescriptor From(
            IDescriptorContext context,
            EnumValueDefinition definition) =>
            new EnumValueDescriptor(context, definition);
    }
}
