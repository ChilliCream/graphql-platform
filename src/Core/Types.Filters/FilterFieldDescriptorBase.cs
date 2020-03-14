using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters
{
    public abstract class FilterFieldDescriptorBase
        : DescriptorBase<FilterFieldDefintion>
    {
        protected FilterFieldDescriptorBase(
            IDescriptorContext context,
            PropertyInfo property)
            : base(context)
        {
            Definition.Property = property
                ?? throw new ArgumentNullException(nameof(property));
            Definition.Name = context.Naming.GetMemberName(
                property, MemberKind.InputObjectField);
            Definition.Description = context.Naming.GetMemberDescription(
                property, MemberKind.InputObjectField);
            Definition.Type = context.Inspector.GetInputReturnType(property);
            Definition.Filters.BindingBehavior =
                context.Options.DefaultBindingBehavior;
        }

        protected override FilterFieldDefintion Definition { get; } =
            new FilterFieldDefintion();

        protected ICollection<FilterOperationDescriptorBase> Filters { get; } =
            new List<FilterOperationDescriptorBase>();

        protected abstract ISet<FilterOperationKind> AllowedOperations { get; }

        protected virtual ISet<FilterOperationKind> ListOperations { get; } =
            new HashSet<FilterOperationKind>
            {
                FilterOperationKind.In,
                FilterOperationKind.NotIn
            };

        protected override void OnCreateDefinition(
            FilterFieldDefintion definition)
        {
            if (Definition.Property is { })
            {
                Context.Inspector.ApplyAttributes(Context, this, Definition.Property);
            }

            var fields = new Dictionary<NameString, FilterOperationDefintion>();
            var handledOperations = new HashSet<FilterOperationKind>();

            AddExplicitFilters(fields, handledOperations);
            OnCompleteFilters(fields, handledOperations);

            Definition.Filters.AddRange(fields.Values);
        }

        private void AddExplicitFilters(
            IDictionary<NameString, FilterOperationDefintion> fields,
            ISet<FilterOperationKind> handledFilterKinds)
        {
            foreach (FilterOperationDefintion filterDefinition in
                Filters.Select(t => t.CreateDefinition()))
            {
                if (!filterDefinition.Ignore)
                {
                    fields[filterDefinition.Name] = filterDefinition;
                }

                handledFilterKinds.Add(filterDefinition.Operation.Kind);
            }
        }

        protected virtual void OnCompleteFilters(
            IDictionary<NameString, FilterOperationDefintion> fields,
            ISet<FilterOperationKind> handledFilterKinds)
        {
            if (Definition.Filters.IsImplicitBinding())
            {
                foreach (FilterOperationKind operationKind in AllowedOperations)
                {
                    AddImplicitOperation(
                        fields,
                        handledFilterKinds,
                        operationKind);
                }
            }
        }

        protected virtual void AddImplicitOperation(
            IDictionary<NameString, FilterOperationDefintion> fields,
            ISet<FilterOperationKind> handledFilterKinds,
            FilterOperationKind operationKind)
        {
            if (handledFilterKinds.Add(operationKind))
            {
                FilterOperationDefintion definition =
                    CreateOperationDefinition(operationKind);
                if (!fields.ContainsKey(definition.Name))
                {
                    fields.Add(definition.Name, definition);
                }
            }
        }

        protected FilterFieldDescriptorBase BindFilters(
            BindingBehavior bindingBehavior)
        {
            Definition.Filters.BindingBehavior = bindingBehavior;
            return this;
        }

        protected ITypeReference RewriteTypeListType()
        {
            ITypeReference reference = Definition.Type;

            if (reference is IClrTypeReference clrRef)
            {
                if (BaseTypes.IsSchemaType(clrRef.Type))
                {
                    return clrRef.WithType(
                        typeof(ListType<>).MakeGenericType(clrRef.Type));
                }
                else
                {
                    return clrRef.WithType(
                        typeof(List<>).MakeGenericType(clrRef.Type));
                }
            }

            if (reference is ISchemaTypeReference schemaRef)
            {
                return schemaRef.WithType(new ListType((IType)schemaRef.Type));
            }

            if (reference is ISyntaxTypeReference syntaxRef)
            {
                return syntaxRef.WithType(new ListTypeNode(syntaxRef.Type));
            }

            throw new NotSupportedException();
        }

        protected ITypeReference RewriteTypeToNullableType()
        {
            ITypeReference reference = Definition.Type;

            if (reference is IClrTypeReference clrRef
                && TypeInspector.Default.TryCreate(
                    clrRef.Type,
                    out Utilities.TypeInfo typeInfo))
            {
                if (BaseTypes.IsSchemaType(typeInfo.ClrType))
                {
                    if (clrRef.Type.IsGenericType
                        && clrRef.Type.GetGenericTypeDefinition() ==
                            typeof(NonNullType<>))
                    {
                        return clrRef.WithType(typeInfo.Components[1]);
                    }
                    return clrRef;
                }
                else
                {
                    if (clrRef.Type.IsValueType)
                    {
                        if (Nullable.GetUnderlyingType(clrRef.Type) == null)
                        {
                            return clrRef.WithType(
                                typeof(Nullable<>).MakeGenericType(clrRef.Type));
                        }
                        return clrRef;
                    }
                    else if (clrRef.Type.IsGenericType
                        && clrRef.Type.GetGenericTypeDefinition() ==
                            typeof(NonNullType<>))
                    {
                        return clrRef.WithType(typeInfo.Components[1]);
                    }
                    return clrRef;
                }
            }

            if (reference is ISchemaTypeReference schemaRef)
            {
                return schemaRef.Type is NonNullType nnt
                    ? schemaRef.WithType(nnt.Type)
                    : schemaRef;
            }

            if (reference is ISyntaxTypeReference syntaxRef)
            {
                return syntaxRef.Type is NonNullTypeNode nnt
                    ? syntaxRef.WithType(nnt.Type)
                    : syntaxRef;
            }

            throw new NotSupportedException();
        }

        protected NameString CreateFieldName(FilterOperationKind kind)
        {
            switch (kind)
            {
                case FilterOperationKind.Equals:
                    return Definition.Name;
                case FilterOperationKind.NotEquals:
                    return Definition.Name + "_not";

                case FilterOperationKind.Contains:
                    return Definition.Name + "_contains";
                case FilterOperationKind.NotContains:
                    return Definition.Name + "_not_contains";

                case FilterOperationKind.In:
                    return Definition.Name + "_in";
                case FilterOperationKind.NotIn:
                    return Definition.Name + "_not_in";

                case FilterOperationKind.StartsWith:
                    return Definition.Name + "_starts_with";
                case FilterOperationKind.NotStartsWith:
                    return Definition.Name + "_not_starts_with";

                case FilterOperationKind.EndsWith:
                    return Definition.Name + "_ends_with";
                case FilterOperationKind.NotEndsWith:
                    return Definition.Name + "_not_ends_with";

                case FilterOperationKind.GreaterThan:
                    return Definition.Name + "_gt";
                case FilterOperationKind.NotGreaterThan:
                    return Definition.Name + "_not_gt";

                case FilterOperationKind.GreaterThanOrEquals:
                    return Definition.Name + "_gte";
                case FilterOperationKind.NotGreaterThanOrEquals:
                    return Definition.Name + "_not_gte";

                case FilterOperationKind.LowerThan:
                    return Definition.Name + "_lt";
                case FilterOperationKind.NotLowerThan:
                    return Definition.Name + "_not_lt";

                case FilterOperationKind.LowerThanOrEquals:
                    return Definition.Name + "_lte";
                case FilterOperationKind.NotLowerThanOrEquals:
                    return Definition.Name + "_not_lte";

                default:
                    throw new NotSupportedException();
            }
        }

        protected virtual ITypeReference RewriteType(
            FilterOperationKind operationKind)
        {
            if (ListOperations.Contains(operationKind))
            {
                return RewriteTypeListType();
            }
            return RewriteTypeToNullableType();
        }

        protected abstract FilterOperationDefintion CreateOperationDefinition(
            FilterOperationKind operationKind);
    }
}
