using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Descriptors
{
    public class InterfaceTypeDescriptor<T>
        : InterfaceTypeDescriptor
        , IInterfaceTypeDescriptor<T>
        , IHasClrType
    {
        protected internal InterfaceTypeDescriptor(IDescriptorContext context)
            : base(context, typeof(T))
        {
            Definition.Fields.BindingBehavior =
                context.Options.DefaultBindingBehavior;
        }

        Type IHasClrType.ClrType => Definition.ClrType;

        protected override void OnCompleteFields(
            IDictionary<NameString, InterfaceFieldDefinition> fields,
            ISet<MemberInfo> handledMembers)
        {
            if (Definition.Fields.IsImplicitBinding())
            {
                FieldDescriptorUtilities.AddImplicitFields(
                    this,
                    p => InterfaceFieldDescriptor
                        .New(Context, p)
                        .CreateDefinition(),
                    fields,
                    handledMembers);
            }

            base.OnCompleteFields(fields, handledMembers);
        }

        public new IInterfaceTypeDescriptor<T> SyntaxNode(
            InterfaceTypeDefinitionNode interfaceTypeDefinition)
        {
            base.SyntaxNode(interfaceTypeDefinition);
            return this;
        }

        public new IInterfaceTypeDescriptor<T> Name(NameString value)
        {
            base.Name(value);
            return this;
        }

        public new IInterfaceTypeDescriptor<T> Description(string value)
        {
            base.Description(value);
            return this;
        }

        public IInterfaceTypeDescriptor<T> BindFields(
            BindingBehavior behavior)
        {
            Definition.Fields.BindingBehavior = behavior;
            return this;
        }

        public IInterfaceTypeDescriptor<T> BindFieldsExplicitly() =>
            BindFields(BindingBehavior.Explicit);

        public IInterfaceTypeDescriptor<T> BindFieldsImplicitly() =>
            BindFields(BindingBehavior.Implicit);

        public IInterfaceFieldDescriptor Field(
            Expression<Func<T, object>> propertyOrMethod)
        {
            if (propertyOrMethod == null)
            {
                throw new ArgumentNullException(nameof(propertyOrMethod));
            }

            MemberInfo member = propertyOrMethod.ExtractMember();
            if (member is PropertyInfo || member is MethodInfo)
            {
                InterfaceFieldDescriptor fieldDescriptor =
                    Fields.FirstOrDefault(t => t.Definition.Member == member);
                if (fieldDescriptor is { })
                {
                    return fieldDescriptor;
                }

                fieldDescriptor = new InterfaceFieldDescriptor(
                    Context, member);
                Fields.Add(fieldDescriptor);
                return fieldDescriptor;
            }

            throw new ArgumentException(
                "A field of an entity can only be a property or a method.",
                nameof(propertyOrMethod));
        }

        public new IInterfaceTypeDescriptor<T> ResolveAbstractType(
            ResolveAbstractType typeResolver)
        {
            base.ResolveAbstractType(typeResolver);
            return this;
        }

        public new IInterfaceTypeDescriptor<T> Directive<TDirective>(
            TDirective directiveInstance)
            where TDirective : class
        {
            base.Directive(directiveInstance);
            return this;
        }

        public new IInterfaceTypeDescriptor<T> Directive<TDirective>()
            where TDirective : class, new()
        {
            base.Directive<TDirective>();
            return this;
        }

        public new IInterfaceTypeDescriptor<T> Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            base.Directive(name, arguments);
            return this;
        }
    }
}
