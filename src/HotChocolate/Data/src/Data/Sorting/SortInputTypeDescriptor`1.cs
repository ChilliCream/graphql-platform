using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
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
        SortInputTypeConfiguration configuration,
        string? scope)
        : base(context, configuration, scope)
    {
    }

    protected override void OnCompleteFields(
        IDictionary<string, SortFieldConfiguration> fields,
        ISet<MemberInfo> handledProperties)
    {
        if (Configuration.Fields.IsImplicitBinding()
            && Configuration.EntityType is { })
        {
            FieldDescriptorUtilities.AddImplicitFields(
                this,
                Configuration.EntityType,
                p =>
                {
                    var config = SortFieldDescriptor
                        .New(Context, Configuration.Scope, p)
                        .CreateConfiguration();
                    config.IsImplicit = true;
                    return config;
                },
                fields,
                handledProperties,
                include: (_, member) => member is PropertyInfo p
                && !handledProperties.Contains(member)
                && IsSortableProperty(p));
        }

        base.OnCompleteFields(fields, handledProperties);
    }

    private bool IsSortableProperty(PropertyInfo property)
    {
        ArgumentNullException.ThrowIfNull(property);

        if (property.GetIndexParameters().Length > 0)
        {
            return false;
        }

        if (!property.CanRead)
        {
            return false;
        }

        if (typeof(IFieldResult).IsAssignableFrom(property.PropertyType))
        {
            return false;
        }

        var runtimeType = Context.TypeInspector.GetReturnType(property, ignoreAttributes: true);

        if (runtimeType.IsArrayOrList)
        {
            return false;
        }

        return IsSortableType(runtimeType, []);
    }

    private bool IsSortableType(IExtendedType runtimeType, HashSet<Type> inspectedTypes)
    {
        ArgumentNullException.ThrowIfNull(runtimeType);
        ArgumentNullException.ThrowIfNull(inspectedTypes);

        TypeReference fieldType;

        try
        {
            fieldType = Convention.GetFieldType(runtimeType.Source);
        }
        catch
        {
            return false;
        }

        if (fieldType is not ExtendedTypeReference { Type.Source: { } sourceType })
        {
            return false;
        }

        if (!TryGetRuntimeTypeFromSortInput(sourceType, out var entityType))
        {
            return true;
        }

        if (!inspectedTypes.Add(entityType))
        {
            return false;
        }

        foreach (var member in Context.TypeInspector.GetMembers(entityType))
        {
            if (member is not PropertyInfo property
                || !IsSortablePropertyCandidate(property))
            {
                continue;
            }

            var propertyType = Context.TypeInspector.GetReturnType(property, ignoreAttributes: true);

            if (propertyType.IsArrayOrList)
            {
                continue;
            }

            if (IsSortableType(propertyType, inspectedTypes))
            {
                inspectedTypes.Remove(entityType);
                return true;
            }
        }

        inspectedTypes.Remove(entityType);
        return false;
    }

    private static bool IsSortablePropertyCandidate(PropertyInfo property)
    {
        ArgumentNullException.ThrowIfNull(property);

        return property.GetIndexParameters().Length == 0
            && property.CanRead
            && !typeof(IFieldResult).IsAssignableFrom(property.PropertyType);
    }

    private static bool TryGetRuntimeTypeFromSortInput(
        Type sourceType,
        [NotNullWhen(true)] out Type? entityType)
    {
        if (sourceType.IsGenericType
            && sourceType.GetGenericTypeDefinition() == typeof(SortInputType<>))
        {
            entityType = sourceType.GenericTypeArguments[0];
            return true;
        }

        entityType = null;
        return false;
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
        if (propertyOrMember.Body is UnaryExpression { NodeType: ExpressionType.ArrayLength })
        {
            var arrayLengthFieldDescriptor =
                SortFieldDescriptor.New(
                    Context,
                    Configuration.Scope,
                    propertyOrMember);
            Fields.Add(arrayLengthFieldDescriptor);
            return arrayLengthFieldDescriptor;
        }

        switch (propertyOrMember.TryExtractMember())
        {
            case PropertyInfo m:
                var fieldDescriptor =
                    Fields.FirstOrDefault(t => t.Configuration.Member == m);

                if (fieldDescriptor is null)
                {
                    fieldDescriptor = SortFieldDescriptor.New(Context, Configuration.Scope, m);
                    Fields.Add(fieldDescriptor);
                }

                return fieldDescriptor;

            case MethodInfo:
                throw new ArgumentException(
                    SortInputTypeDescriptor_Field_OnlyProperties,
                    nameof(propertyOrMember));

            default:
                fieldDescriptor = SortFieldDescriptor
                    .New(Context, Configuration.Scope, propertyOrMember);
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
                Fields.FirstOrDefault(t => t.Configuration.Member == p);

            if (fieldDescriptor is null)
            {
                fieldDescriptor = IgnoreSortFieldDescriptor.New(Context, Configuration.Scope, p);
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
