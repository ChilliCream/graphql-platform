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

        public EnumValueDescriptor(IDescriptorContext context, object value)
            : base(context)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            Definition.Name = context.Naming.GetEnumValueName(value);
            Definition.Value = value;
            Definition.Description = context.Naming.GetEnumValueDescription(value);
            Definition.Member = context.Inspector.GetEnumValueMember(value);

            if (context.Naming.IsDeprecated(value, out string reason))
            {
                Deprecated(reason);
            }
        }

        internal protected override EnumValueDefinition Definition { get; } =
            new EnumValueDefinition();

        protected override void OnCreateDefinition(EnumValueDefinition definition)
        {
            base.OnCreateDefinition(definition);

            if (Definition.Member is { })
            {
                Context.Inspector.ApplyAttributes(this, Definition.Member);
            }
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
                AddDeprectedDirective(reason);
                return this;
            }
        }

        public IEnumValueDescriptor Deprecated()
        {
            Definition.DeprecationReason =
                WellKnownDirectives.DeprecationDefaultReason;
            AddDeprectedDirective(null);
            return this;
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

        public IEnumValueDescriptor Directive<T>(T directiveInstance)
            where T : class
        {
            Definition.AddDirective(directiveInstance);
            return this;
        }

        public IEnumValueDescriptor Directive<T>()
            where T : class, new()
        {
            Definition.AddDirective(new T());
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
    }
}
