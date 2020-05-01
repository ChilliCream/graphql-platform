using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Filters.Conventions;
using HotChocolate.Types.Filters.Extensions;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters
{
    public abstract class FilterFieldDescriptorBase
        : DescriptorBase<FilterFieldDefintion>
    {
        protected FilterFieldDescriptorBase(
            FilterKind filterKind,
            IDescriptorContext context,
            PropertyInfo property,
            IFilterConvention filterConventions)
            : base(context)
        {
            FilterConvention = filterConventions;

            Definition.Kind = filterKind;
            Definition.Property = property
                ?? throw new ArgumentNullException(nameof(property));
            Definition.Name = context.Naming.GetMemberName(
                property, MemberKind.InputObjectField);
            Definition.Description = context.Naming.GetMemberDescription(
                property, MemberKind.InputObjectField);
            Definition.Type = context.Inspector.GetInputReturnType(property);
            Definition.Filters.BindingBehavior =
                context.Options.DefaultBindingBehavior;

            AllowedOperations = FilterConvention.GetAllowedOperations(Definition);
        }

        internal protected sealed override FilterFieldDefintion Definition { get; } =
            new FilterFieldDefintion();

        protected readonly IFilterConvention FilterConvention;

        protected ICollection<FilterOperationDescriptorBase> Filters { get; } =
            new List<FilterOperationDescriptorBase>();

        protected IReadOnlyCollection<FilterOperationKind> AllowedOperations { get; }

        protected virtual ISet<FilterOperationKind> ListOperations { get; } =
            new HashSet<FilterOperationKind>
            {
                FilterOperationKind.In,
                FilterOperationKind.NotIn
            };

        public TDesc Allow<TDesc>(
            FilterOperationKind kind,
            Func<FilterFieldDefintion, TDesc> factory) where TDesc : FilterOperationDescriptorBase
        {
            return Filters.GetOrAddOperation(kind, () => factory(Definition));
        }

        protected void Name(NameString value)
        {
            Definition.Name = value.EnsureNotEmpty(nameof(value));
        }

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

            base.OnCreateDefinition(definition);
        }

        private void AddExplicitFilters(
            IDictionary<NameString, FilterOperationDefintion> fields,
            ISet<FilterOperationKind> handledFilterKinds)
        {
            foreach (FilterOperationDefintion filterDefinition in
                Filters.Select(t => t.CreateDefinition()).Where(x => x.Operation is { }))
            {
                if (!filterDefinition.Ignore)
                {
                    fields[filterDefinition.Name] = filterDefinition;
                }

                handledFilterKinds.Add(filterDefinition.Operation!.Kind);
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

        protected void Type<TInputType>()
            where TInputType : IInputType
        {
            Type(typeof(TInputType));
        }

        protected void Type<TInputType>(TInputType inputType)
            where TInputType : class, IInputType
        {
            if (inputType == null)
            {
                throw new ArgumentNullException(nameof(inputType));
            }

            if (!inputType.IsInputType())
            {
                // TODO : resource
                throw new ArgumentException(
                    "TypeResources.ObjectFieldDescriptorBase_FieldType");
            }

            Definition.Type = new SchemaTypeReference(inputType);
        }

        protected void Type(Type type)
        {
            Type extractedType = Context.Inspector.ExtractType(type);

            if (Context.Inspector.IsSchemaType(extractedType)
                && !typeof(IInputType).IsAssignableFrom(extractedType))
            {
                // TODO : resource
                throw new ArgumentException(
                    "TypeResources.ObjectFieldDescriptorBase_FieldType");
            }

            Definition.SetMoreSpecificType(
                type,
                TypeContext.Input);
        }

        protected void Type(ITypeNode typeNode)
        {
            if (typeNode == null)
            {
                throw new ArgumentNullException(nameof(typeNode));
            }
            Definition.SetMoreSpecificType(typeNode, TypeContext.Input);
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
                    Type type = clrRef.Type;
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
            if (typeof(ISingleFilter).IsAssignableFrom(Definition.Property.DeclaringType))
            {
                Definition.Name = FilterConvention.GetArrayFilterPropertyName();
            }
            return FilterConvention.CreateFieldName(Definition, kind);
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
