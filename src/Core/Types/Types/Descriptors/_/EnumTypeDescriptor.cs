using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors
{
    public class EnumTypeDescriptor
        : IEnumTypeDescriptor
        , IDescriptionFactory<EnumTypeDescription>
    {
        public EnumTypeDescriptor(NameString name)
        {
            EnumDescription.Name = name.EnsureNotEmpty(nameof(name));
        }

        public EnumTypeDescriptor(Type enumType)
        {
            EnumDescription.ClrType = enumType
                ?? throw new ArgumentNullException(nameof(enumType));
            EnumDescription.Name = enumType.GetGraphQLName();
            EnumDescription.Description = enumType.GetGraphQLDescription();
        }

        protected EnumTypeDescription EnumDescription { get; } =
            new EnumTypeDescription();

        protected ICollection<EnumValueDescriptor> ValueDescriptors { get; } =
            new List<EnumValueDescriptor>();

        public EnumTypeDescription CreateDescription()
        {
            CompleteValues();
            return EnumDescription;
        }

        DescriptionBase IDescriptionFactory.CreateDescription() =>
            CreateDescription();

        private void CompleteValues()
        {
            var values = new Dictionary<object, EnumValueDescription>();
            OnCompleteValues(values);
            UpdateValues(values.Values);
        }

        protected virtual void OnCompleteValues(
            IDictionary<object, EnumValueDescription> values)
        {
            AddExplicitValues(values);
            AddImplicitValues(values);
        }

        protected void AddExplicitValues(
            IDictionary<object, EnumValueDescription> values)
        {
            foreach (EnumValueDescription valueDescription in
                ValueDescriptors.Select(t => t.CreateDescription()))
            {
                values[valueDescription.Value] = valueDescription;
            }
        }

        protected void AddImplicitValues(
            IDictionary<object, EnumValueDescription> values)
        {
            if (EnumDescription.Values.IsImplicitBinding()
                && EnumDescription.ClrType != typeof(object)
                && EnumDescription.ClrType.IsEnum)
            {
                foreach (object value in Enum.GetValues(
                    EnumDescription.ClrType))
                {
                    EnumValueDescription description =
                        new EnumValueDescriptor(value)
                            .CreateDescription();

                    if (!values.ContainsKey(description.Value))
                    {
                        values.Add(description.Value, description);
                    }
                }
            }
        }

        private void UpdateValues(IEnumerable<EnumValueDescription> values)
        {
            EnumDescription.Values.Clear();

            foreach (EnumValueDescription value in values)
            {
                EnumDescription.Values.Add(value);
            }
        }

        public IEnumTypeDescriptor SyntaxNode(
            EnumTypeDefinitionNode enumTypeDefinition)
        {
            EnumDescription.SyntaxNode = enumTypeDefinition;
            return this;
        }

        public IEnumTypeDescriptor Name(NameString value)
        {
            EnumDescription.Name = value.EnsureNotEmpty(nameof(value));
            return this;
        }

        public IEnumTypeDescriptor Description(string value)
        {
            EnumDescription.Description = value;
            return this;
        }

        public IEnumTypeDescriptor BindItems(
            BindingBehavior behavior)
        {
            EnumDescription.Values.BindingBehavior = behavior;
            return this;
        }

        public IEnumValueDescriptor Item<T>(T value)
        {
            if (EnumDescription.ClrType == null)
            {
                EnumDescription.ClrType = typeof(T);
            }

            var descriptor = new EnumValueDescriptor(value);
            ValueDescriptors.Add(descriptor);
            return descriptor;
        }

        public IEnumTypeDescriptor Directive<T>(T instance)
            where T : class
        {
            EnumDescription.AddDirective(instance);
            return this;
        }

        public IEnumTypeDescriptor Directive<T>()
            where T : class, new()
        {
            EnumDescription.AddDirective(new T());
            return this;
        }

        public IEnumTypeDescriptor Directive(
            NameString name, params ArgumentNode[] arguments)
        {
            EnumDescription.AddDirective(name, arguments);
            return this;
        }
    }
}
