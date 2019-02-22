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

        public new IEnumTypeDescriptor<T> SyntaxNode(
            EnumTypeDefinitionNode enumTypeDefinition)
        {
            base.SyntaxNode(enumTypeDefinition);
            return this;
        }

        public new IEnumTypeDescriptor<T> Name(NameString value)
        {
            base.Name(value);
            return this;
        }

        public new IEnumTypeDescriptor<T> Description(string value)
        {
            base.Description(value);
            return this;
        }

        public IEnumTypeDescriptor<T> BindItems(BindingBehavior behavior)
        {
            base.BindItems(behavior);
            return this;
        }

        public IEnumTypeDescriptor<T> Item(T value)
        {
            base.Item(value);
            return this;
        }

        public new IEnumTypeDescriptor<T> Directive<TDirective>(
            TDirective directiveInstance)
            where TDirective : class
        {
            base.Directive(directiveInstance);
            return this;
        }

        public new IEnumTypeDescriptor<T> Directive<TDirective>()
            where TDirective : class, new()
        {
            base.Directive<TDirective>();
            return this;
        }

        public new IEnumTypeDescriptor<T> Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            base.Directive(name, arguments);
            return this;
        }
    }
}
