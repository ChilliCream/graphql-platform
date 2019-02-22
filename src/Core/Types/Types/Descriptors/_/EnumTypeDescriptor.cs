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

        #region IDescriptionFactory

        public EnumTypeDescription CreateDescription()
        {
            CompleteValues();
            return EnumDescription;
        }

        DescriptionBase IDescriptionFactory.CreateDescription() =>
            CreateDescription();

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

            foreach (EnumValueDescription valueDescription in
                valueToDesc.Values)
            {
                values[valueDescription.Name] = valueDescription;
            }

            EnumDescription.Values.Clear();
            EnumDescription.Values.AddRange(values.Values);
        }

        protected void AddImplicitValues(
            Dictionary<object, EnumValueDescription> valueToDesc)
        {
            if (EnumDescription.Values.IsImplicitBinding())
            {
                if (EnumDescription.ClrType != null
                    && EnumDescription.ClrType.IsEnum)
                {
                    foreach (object o in Enum.GetValues(
                        EnumDescription.ClrType))
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

        #endregion

        public IEnumTypeDescriptor SyntaxNode(
            EnumTypeDefinitionNode typeDefinition)
        {
            EnumDescription.SyntaxNode = typeDefinition;
            return this;
        }

        public IEnumTypeDescriptor Name(NameString value)
        {
            EnumDescription.Name = value;
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
