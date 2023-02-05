using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;
using HotChocolate.Types.Sorting.Extensions;
using HotChocolate.Types.Sorting.Properties;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Sorting;

[Obsolete("Use HotChocolate.Data.")]
public class SortInputTypeDescriptor<T>
    : DescriptorBase<SortInputTypeDefinition>
    , ISortInputTypeDescriptor<T>
{
    protected SortInputTypeDescriptor(
        IDescriptorContext context,
        Type entityType)
        : base(context)
    {
        var convention = context.GetSortingNamingConvention();
        Definition.EntityType = entityType ?? throw new ArgumentNullException(nameof(entityType));
        Definition.RuntimeType = typeof(object);
        Definition.Name = convention.GetSortingTypeName(context, entityType);
        Definition.Description = context.Naming.GetTypeDescription(entityType, TypeKind.Object);
    }

    protected internal sealed override SortInputTypeDefinition Definition { get; protected set; } =
        new();

    protected ICollection<SortOperationDescriptorBase> Fields { get; } =
        new List<SortOperationDescriptorBase>();


    public ISortInputTypeDescriptor<T> Name(string value)
    {
        Definition.Name = value;
        return this;
    }

    public ISortInputTypeDescriptor<T> Description(
        string value)
    {
        Definition.Description = value;
        return this;
    }

    public ISortInputTypeDescriptor<T> Directive<TDirective>(
        TDirective directiveInstance)
        where TDirective : class
    {
        Definition.AddDirective(directiveInstance, Context.TypeInspector);
        return this;
    }

    public ISortInputTypeDescriptor<T> Directive<TDirective>()
        where TDirective : class, new()
    {
        Definition.AddDirective(new TDirective(), Context.TypeInspector);
        return this;
    }

    public ISortInputTypeDescriptor<T> Directive(
        string name,
        params ArgumentNode[] arguments)
    {
        Definition.AddDirective(name, arguments);
        return this;
    }


    public ISortInputTypeDescriptor<T> BindFields(
        BindingBehavior behavior)
    {
        Definition.Fields.BindingBehavior = behavior;
        return this;
    }

    public ISortInputTypeDescriptor<T> BindFieldsImplicitly() =>
        BindFields(BindingBehavior.Implicit);

    public ISortInputTypeDescriptor<T> BindFieldsExplicitly() =>
        BindFields(BindingBehavior.Explicit);

    public ISortOperationDescriptor Sortable(
        Expression<Func<T, IComparable>> property)
    {
        if (property.ExtractMember() is PropertyInfo p)
        {
            return Fields.GetOrAddDescriptor(p,
                () => SortOperationDescriptor.CreateOperation(p, Context));
        }

        // TODO : resources
        throw new ArgumentException(
            SortingResources.SortInputTypeDescriptor_Ignore_OnlyPopertiesAreAllowed,
            nameof(property));
    }

    public ISortObjectOperationDescriptor<TObject> SortableObject<TObject>(
        Expression<Func<T, TObject>> property)
        where TObject : class
    {
        if (property.ExtractMember() is PropertyInfo p)
        {
            return Fields.GetOrAddDescriptor(p,
                () => SortObjectOperationDescriptor<TObject>.CreateOperation(p, Context));
        }

        // TODO : resources
        throw new ArgumentException(
            SortingResources.SortInputTypeDescriptor_Ignore_OnlyPopertiesAreAllowed,
            nameof(property));
    }

    public ISortInputTypeDescriptor<T> Ignore(Expression<Func<T, object>> property)
    {
        if (property.ExtractMember() is PropertyInfo p)
        {
            Fields.GetOrAddDescriptor(
                p,
                () => IgnoredSortingFieldDescriptor.CreateOperation(p, Context));
            return this;
        }

        throw new ArgumentException(
            SortingResources.SortInputTypeDescriptor_Ignore_OnlyPopertiesAreAllowed,
            nameof(property));
    }

    protected override void OnCreateDefinition(
        SortInputTypeDefinition definition)
    {
        if (!Definition.AttributesAreApplied && Definition.EntityType is not null)
        {
            Context.TypeInspector.ApplyAttributes(Context, this, Definition.EntityType);
            Definition.AttributesAreApplied = true;
        }

        var fields = new Dictionary<string, SortOperationDefintion>(StringComparer.Ordinal);
        var handledProperties = new HashSet<PropertyInfo>();

        var explicitFields =
            Fields.Select(x => x.CreateDefinition()).ToList();

        FieldDescriptorUtilities.AddExplicitFields(
            explicitFields.Where(t => !t.Ignore),
            f => f.Operation?.Property,
            fields,
            handledProperties);

        foreach (var field in explicitFields.Where(t => t.Ignore))
        {
            if (field.Operation?.Property is { } property)
            {
                handledProperties.Add(property);
            }
        }

        OnCompleteFields(fields, handledProperties);

        Definition.Fields.AddRange(fields.Values);
    }

    protected virtual void OnCompleteFields(
        IDictionary<string, SortOperationDefintion> fields,
        ISet<PropertyInfo> handledProperties)
    {
        if (!Definition.Fields.IsImplicitBinding() ||
            Definition.EntityType == typeof(object))
        {
            return;
        }

        foreach (var member in Context.TypeInspector.GetMembers(Definition.EntityType!))
        {
            if (member is PropertyInfo property)
            {
                if (handledProperties.Contains(property))
                {
                    continue;
                }

                if (TryCreateImplicitSorting(property, out var definition)
                    && !fields.ContainsKey(definition.Name))
                {
                    fields[definition.Name] = definition;
                }
            }
        }
    }

    private bool TryCreateImplicitSorting(
        PropertyInfo property,
        [NotNullWhen(true)] out SortOperationDefintion? definition)
    {
        var type = property.PropertyType;

        if (type.IsGenericType
            && System.Nullable.GetUnderlyingType(type) is { } nullableType)
        {
            type = nullableType;
        }

        if (typeof(IComparable).IsAssignableFrom(type))
        {
            definition = SortOperationDescriptor
                .CreateOperation(property, Context)
                .CreateDefinition();
            return true;
        }

        if (Context.TypeInspector.TryCreateTypeInfo(type, out var typeInfo) &&
            typeInfo.GetExtendedType()?.IsList is { } isList)
        {
            if (type.IsClass && !isList)
            {
                definition = SortObjectOperationDescriptor
                    .CreateOperation(property, Context)
                    .CreateDefinition();
                return true;
            }
        }

        definition = null;
        return false;
    }

    public static SortInputTypeDescriptor<T> New(IDescriptorContext context, Type entityType)
        => new(context, entityType);
}
