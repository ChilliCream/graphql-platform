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
        private readonly IFilterNamingConvention _namingConvention;

        protected FilterFieldDescriptorBase(
            IDescriptorContext context,
            PropertyInfo property)
            : base(context)
        {
            _namingConvention = context.GetConventionOrDefault<IFilterNamingConvention>(
                FilterNamingConventionSnakeCase.Default);
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

        internal protected sealed override FilterFieldDefintion Definition { get; } =
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
            return RewriteTypeToNullableType(reference);
        }

        protected static ITypeReference RewriteTypeToNullableType(ITypeReference reference)
        {

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
                    var type = clrRef.Type;
                    if (type.IsGenericType &&
                        System.Nullable.GetUnderlyingType(type) is Type nullableType)
                    {
                        type = nullableType;
                    }
                    if (type.IsValueType)
                    {
                        return clrRef.WithType(
                            typeof(Nullable<>).MakeGenericType(type));
                    }
                    else if (type.IsGenericType
                        && type.GetGenericTypeDefinition() ==
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
                    ? schemaRef.WithType(nnt)
                    : schemaRef;
            }

            if (reference is ISyntaxTypeReference syntaxRef)
            {
                return syntaxRef.Type is NonNullTypeNode nnt
                    ? syntaxRef.WithType(nnt)
                    : syntaxRef;
            }

            throw new NotSupportedException();
        }

        protected NameString CreateFieldName(FilterOperationKind kind)
        {
            return _namingConvention.CreateFieldName(Definition, kind);
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
