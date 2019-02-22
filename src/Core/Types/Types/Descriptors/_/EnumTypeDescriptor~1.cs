using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors
{
    public class EnumTypeDescriptor<T>
        : EnumTypeDescriptor
        , IEnumTypeDescriptor<T>
    {
        public EnumTypeDescriptor()
            : base(typeof(T))
        {
        }

        #region IEnumTypeDescriptor<T>

        IEnumTypeDescriptor<T> IEnumTypeDescriptor<T>.SyntaxNode(
            EnumTypeDefinitionNode typeDefinition)
        {
            EnumDescription.SyntaxNode = typeDefinition;
            return this;
        }

        IEnumTypeDescriptor<T> IEnumTypeDescriptor<T>.Name(NameString value)
        {
            EnumDescription.Name = value;
            return this;
        }

        IEnumTypeDescriptor<T> IEnumTypeDescriptor<T>.Description(
            string description)
        {
            EnumDescription.Description = description;
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
            NameString name,
            params ArgumentNode[] arguments)
        {
            EnumDescription.Directives.AddDirective(name, arguments);
            return this;
        }

        #endregion
    }
}
