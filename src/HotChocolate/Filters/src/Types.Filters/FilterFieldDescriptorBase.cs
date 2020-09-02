using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

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
            _namingConvention = context.GetFilterNamingConvention();
            Definition.Property = property
                ?? throw new ArgumentNullException(nameof(property));
            Definition.Name = context.Naming.GetMemberName(
                property,
                MemberKind.InputObjectField);
            Definition.Description = context.Naming.GetMemberDescription(
                property,
                MemberKind.InputObjectField);
            Definition.Type = context.TypeInspector.GetReturnTypeRef(property);
            Definition.Filters.BindingBehavior =
                context.Options.DefaultBindingBehavior;
            _namingConvention = context.GetFilterNamingConvention();
        }

        protected internal sealed override FilterFieldDefintion Definition { get; protected set; } =
            new FilterFieldDefintion();

        protected ICollection<FilterOperationDescriptorBase> Filters { get; } =
            new List<FilterOperationDescriptorBase>();

        protected abstract ISet<FilterOperationKind> AllowedOperations { get; }

        protected virtual ISet<FilterOperationKind> ListOperations { get; } =
            new HashSet<FilterOperationKind> { FilterOperationKind.In, FilterOperationKind.NotIn };

        protected override void OnCreateDefinition(
            FilterFieldDefintion definition)
        {
            if (Definition.Property is { })
            {
                Context.TypeInspector.ApplyAttributes(Context, this, Definition.Property);
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
            if (inputType is null)
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
            Type extractedType = Context.TypeInspector.ExtractNamedType(type);

            if (Context.TypeInspector.IsSchemaType(extractedType)
                && !typeof(IInputType).IsAssignableFrom(extractedType))
            {
                // TODO : resource
                throw new ArgumentException(
                    "TypeResources.ObjectFieldDescriptorBase_FieldType");
            }

            Definition.SetMoreSpecificType(
                Context.TypeInspector.GetType(extractedType),
                TypeContext.Input);
        }

        protected void Type(ITypeNode typeNode)
        {
            if (typeNode is null)
            {
                throw new ArgumentNullException(nameof(typeNode));
            }

            Definition.SetMoreSpecificType(typeNode, TypeContext.Input);
        }

        protected ITypeReference RewriteTypeListType()
        {
            ITypeReference reference = Definition.Type;

            if (reference is ExtendedTypeReference extendedRef)
            {
                Span<bool?> buffer = stackalloc bool?[32];
                Context.TypeInspector.CollectNullability(
                    extendedRef.Type, buffer.Slice(1), out int written);

                if (extendedRef.Type.IsSchemaType)
                {
                    IExtendedType listType = Context.TypeInspector.GetType(
                        typeof(ListType<>).MakeGenericType(extendedRef.Type.Source),
                        buffer.Slice(0, written + 1));
                    return extendedRef.WithType(listType);
                }

                IExtendedType runtimeListType = Context.TypeInspector.GetType(
                    typeof(List<>).MakeGenericType(extendedRef.Type.Source),
                    buffer.Slice(0, written + 1));
                return extendedRef.WithType(runtimeListType);
            }

            if (reference is SchemaTypeReference schemaRef)
            {
                return schemaRef.WithType(new ListType((IType)schemaRef.Type));
            }

            if (reference is SyntaxTypeReference syntaxRef)
            {
                return syntaxRef.WithType(new ListTypeNode(syntaxRef.Type));
            }

            throw new NotSupportedException();
        }

        protected ITypeReference RewriteTypeToNullableType()
        {
            ITypeReference reference = Definition.Type;
            return RewriteTypeToNullableType(reference, Context.TypeInspector);
        }

        protected static ITypeReference RewriteTypeToNullableType(
            ITypeReference reference,
            ITypeInspector typeInspector)
        {
            if (reference is ExtendedTypeReference extendedTypeRef)
            {
                return extendedTypeRef.Type.IsNullable
                    ? extendedTypeRef
                    : extendedTypeRef.WithType(
                        typeInspector.ChangeNullability(extendedTypeRef.Type, true));
            }

            if (reference is SchemaTypeReference schemaRef)
            {
                return schemaRef.Type is NonNullType nnt
                    ? schemaRef.WithType(nnt.Type)
                    : schemaRef;
            }

            if (reference is SyntaxTypeReference syntaxRef)
            {
                return syntaxRef.Type is NonNullTypeNode nnt
                    ? syntaxRef.WithType(nnt.Type)
                    : syntaxRef;
            }

            throw new NotSupportedException();
        }

        protected NameString CreateFieldName(FilterOperationKind kind)
        {
            if (typeof(ISingleFilter).IsAssignableFrom(Definition.Property.DeclaringType))
            {
                Definition.Name = _namingConvention.ArrayFilterPropertyName;
            }

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
