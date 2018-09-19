using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class EnumTypeDescriptor
        : IEnumTypeDescriptor
        , IDescriptionFactory<EnumTypeDescription>
    {
        public EnumTypeDescriptor(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(
                    "The name cannot be null or empty.",
                    nameof(name));
            }

            EnumDescription.Name = name;
        }

        public EnumTypeDescriptor(Type enumType)
        {
            EnumDescription.NativeType = enumType
                ?? throw new ArgumentNullException(nameof(enumType));
            EnumDescription.Name = enumType.GetGraphQLName();
        }

        protected List<EnumValueDescriptor> Values { get; } =
            new List<EnumValueDescriptor>();

        protected EnumTypeDescription EnumDescription { get; } =
            new EnumTypeDescription();

        public EnumTypeDescription CreateDescription()
        {
            CompleteValues();
            return EnumDescription;
        }

        protected void CompleteValues()
        {
            var valueToDesc = new Dictionary<object, EnumValueDescription>();

            foreach (EnumValueDescription valueDescription in
                Values.Select(t => t.CreateDescription()))
            {
                valueToDesc[valueDescription.Value] = valueDescription;
            }

            AddImplicitValues(valueToDesc);

            var values = new Dictionary<string, EnumValueDescription>();

            foreach (EnumValueDescription valueDescription in valueToDesc.Values)
            {
                values[valueDescription.Name] = valueDescription;
            }

            EnumDescription.Values.Clear();
            EnumDescription.Values.AddRange(values.Values);
        }

        protected void AddImplicitValues(Dictionary<object, EnumValueDescription> valueToDesc)
        {
            if (EnumDescription.ValueBindingBehavior == BindingBehavior.Implicit)
            {
                if (EnumDescription.NativeType != null
                    && EnumDescription.NativeType.IsEnum)
                {
                    foreach (object o in Enum.GetValues(
                        EnumDescription.NativeType))
                    {
                        EnumValueDescription description =
                            new EnumValueDescriptor(o)
                                .CreateDescription();
                        if (!valueToDesc.ContainsKey(description.Value))
                        {
                            valueToDesc[description.Value] = description;
                        }
                    }
                }
            }
        }

        protected void SyntaxNode(EnumTypeDefinitionNode syntaxNode)
        {
            EnumDescription.SyntaxNode = syntaxNode;
        }

        protected void Name(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(
                    "The name cannot be null or empty.",
                    nameof(name));
            }

            if (!ValidationHelper.IsTypeNameValid(name))
            {
                throw new ArgumentException(
                    "The specified name is not a valid GraphQL type name.",
                    nameof(name));
            }

            EnumDescription.Name = name;
        }

        protected void Description(string description)
        {
            EnumDescription.Description = description;
        }

        protected EnumValueDescriptor Item<T>(T value)
        {
            if (EnumDescription.NativeType == null)
            {
                EnumDescription.NativeType = typeof(T);
            }

            var descriptor = new EnumValueDescriptor(value);
            Values.Add(descriptor);
            return descriptor;
        }

        protected void BindItems(BindingBehavior bindingBehavior)
        {
            EnumDescription.ValueBindingBehavior = bindingBehavior;
        }

        #region IEnumTypeDescriptor

        IEnumTypeDescriptor IEnumTypeDescriptor.SyntaxNode(
            EnumTypeDefinitionNode syntaxNode)
        {
            SyntaxNode(syntaxNode);
            return this;
        }

        IEnumTypeDescriptor IEnumTypeDescriptor.Name(string name)
        {
            Name(name);
            return this;
        }

        IEnumTypeDescriptor IEnumTypeDescriptor.Description(string description)
        {
            Description(description);
            return this;
        }

        IEnumValueDescriptor IEnumTypeDescriptor.Item<T>(T value)
        {
            return Item(value);
        }

        IEnumTypeDescriptor IEnumTypeDescriptor.BindItems(
            BindingBehavior bindingBehavior)
        {
            BindItems(bindingBehavior);
            return this;
        }

        IEnumTypeDescriptor IEnumTypeDescriptor.Directive<T>(T directive)
        {
            EnumDescription.Directives.AddDirective(directive);
            return this;
        }

        IEnumTypeDescriptor IEnumTypeDescriptor.Directive<T>()
        {
            EnumDescription.Directives.AddDirective(new T());
            return this;
        }

        IEnumTypeDescriptor IEnumTypeDescriptor.Directive(
            string name,
            params ArgumentNode[] arguments)
        {
            EnumDescription.Directives.AddDirective(name, arguments);
            return this;
        }

        #endregion
    }

    internal class EnumTypeDescriptor<T>
        : EnumTypeDescriptor
        , IEnumTypeDescriptor<T>
    {
        public EnumTypeDescriptor()
            : base(typeof(T))
        {
        }

        #region IEnumTypeDescriptor<T>

        IEnumTypeDescriptor<T> IEnumTypeDescriptor<T>.SyntaxNode(
            EnumTypeDefinitionNode syntaxNode)
        {
            SyntaxNode(syntaxNode);
            return this;
        }

        IEnumTypeDescriptor<T> IEnumTypeDescriptor<T>.Name(string name)
        {
            Name(name);
            return this;
        }

        IEnumTypeDescriptor<T> IEnumTypeDescriptor<T>.Description(
            string description)
        {
            Description(description);
            return this;
        }

        IEnumTypeDescriptor<T> IEnumTypeDescriptor<T>.BindItems(
            BindingBehavior bindingBehavior)
        {
            BindItems(bindingBehavior);
            return this;
        }

        IEnumValueDescriptor IEnumTypeDescriptor<T>.Item(T value)
        {
            return Item(value);
        }

        IEnumTypeDescriptor<T> IEnumTypeDescriptor<T>.Directive<TDirective>(
            TDirective directive)
        {
            EnumDescription.Directives.AddDirective(directive);
            return this;
        }

        IEnumTypeDescriptor<T> IEnumTypeDescriptor<T>.Directive<TDirective>()
        {
            EnumDescription.Directives.AddDirective(new TDirective());
            return this;
        }

        IEnumTypeDescriptor<T> IEnumTypeDescriptor<T>.Directive(
            string name, params ArgumentNode[] arguments)
        {
            EnumDescription.Directives.AddDirective(name, arguments);
            return this;
        }

        #endregion
    }
}
