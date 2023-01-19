using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;
using HotChocolate.Utilities;
using static HotChocolate.Data.DataResources;

namespace HotChocolate.Data.Sorting;

public class SortInputTypeDescriptor<T>
    : SortInputTypeDescriptor
    , ISortInputTypeDescriptor<T>
{
    protected internal SortInputTypeDescriptor(
        IDescriptorContext context,
        Type entityType,
        string? scope)
        : base(context, entityType, scope)
    {
    }

    protected internal SortInputTypeDescriptor(
        IDescriptorContext context,
        string? scope)
        : base(context, scope)
    {
    }

    protected internal SortInputTypeDescriptor(
        IDescriptorContext context,
        SortInputTypeDefinition definition,
        string? scope)
        : base(context, definition, scope)
    {
    }

    protected override void OnCompleteFields(
        IDictionary<string, SortFieldDefinition> fields,
        ISet<MemberInfo> handledProperties)
    {
        if (Definition.Fields.IsImplicitBinding() &&
            Definition.EntityType is { })
        {
            FieldDescriptorUtilities.AddImplicitFields(
                this,
                Definition.EntityType,
                p => SortFieldDescriptor
                    .New(Context, Definition.Scope, p)
                    .CreateDefinition(),
                fields,
                handledProperties,
                include: (_, member) => member is PropertyInfo &&
                    !handledProperties.Contains(member) &&
                    !Context.TypeInspector.GetReturnType(member).IsArrayOrList);
        }

        base.OnCompleteFields(fields, handledProperties);
    }

    /// <inheritdoc />
    public new ISortInputTypeDescriptor<T> Name(string value)
    {
        base.Name(value);
        return this;
    }

    /// <inheritdoc />
    public new ISortInputTypeDescriptor<T> Description(string? value)
    {
        base.Description(value);
        return this;
    }

    /// <inheritdoc />
    public new ISortInputTypeDescriptor<T> BindFields(BindingBehavior bindingBehavior)
    {
        base.BindFields(bindingBehavior);
        return this;
    }

    /// <inheritdoc />
    public new ISortInputTypeDescriptor<T> BindFieldsExplicitly()
    {
        base.BindFieldsExplicitly();
        return this;
    }

    /// <inheritdoc />
    public new ISortInputTypeDescriptor<T> BindFieldsImplicitly()
    {
        base.BindFieldsImplicitly();
        return this;
    }

    /// <inheritdoc />
    public ISortFieldDescriptor Field<TField>(Expression<Func<T, TField>> propertyOrMember)
    {
        switch (propertyOrMember.TryExtractMember())
        {
            case PropertyInfo m:
                var fieldDescriptor =
                    Fields.FirstOrDefault(t => t.Definition.Member == m);

                if (fieldDescriptor is null)
                {
                    fieldDescriptor = SortFieldDescriptor.New(Context, Definition.Scope, m);
                    Fields.Add(fieldDescriptor);
                }

                return fieldDescriptor;

            case MethodInfo:
                throw new ArgumentException(
                    SortInputTypeDescriptor_Field_OnlyProperties,
                    nameof(propertyOrMember));

            default:
                fieldDescriptor = SortFieldDescriptor
                    .New(Context, Definition.Scope, propertyOrMember);
                Fields.Add(fieldDescriptor);
                return fieldDescriptor;
        }
    }

    /// <inheritdoc />
    public new ISortInputTypeDescriptor<T> Ignore(string name)
    {
        base.Ignore(name);
        return this;
    }

    /// <inheritdoc />
    public ISortInputTypeDescriptor<T> Ignore(Expression<Func<T, object?>> propertyOrMember)
    {
        if (propertyOrMember.ExtractMember() is PropertyInfo p)
        {
            var fieldDescriptor =
                Fields.FirstOrDefault(t => t.Definition.Member == p);

            if (fieldDescriptor is null)
            {
                fieldDescriptor = IgnoreSortFieldDescriptor.New(Context, Definition.Scope, p);
                Fields.Add(fieldDescriptor);
            }

            return this;
        }

        throw new ArgumentException(
            SortInputTypeDescriptor_Field_OnlyProperties,
            nameof(propertyOrMember));
    }

    public new ISortInputTypeDescriptor<T> Directive<TDirective>(
        TDirective directive)
        where TDirective : class
    {
        base.Directive(directive);
        return this;
    }

    public new ISortInputTypeDescriptor<T> Directive<TDirective>()
        where TDirective : class, new()
    {
        base.Directive<TDirective>();
        return this;
    }

    public new ISortInputTypeDescriptor<T> Directive(string name, params ArgumentNode[] arguments)
    {
        base.Directive(name, arguments);
        return this;
    }
}
