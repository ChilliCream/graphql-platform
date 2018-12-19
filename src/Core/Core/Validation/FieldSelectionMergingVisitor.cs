using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Utilities;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Introspection;

namespace HotChocolate.Validation
{
    internal sealed class FieldSelectionMergingVisitor
        : QueryVisitorErrorBase
    {
        private readonly Dictionary<SelectionSetNode, List<FieldInfo>> _fieldSelectionSets =
            new Dictionary<SelectionSetNode, List<FieldInfo>>();
        private readonly HashSet<string> _visitedFragments =
            new HashSet<string>();

        public FieldSelectionMergingVisitor(ISchema schema)
            : base(schema)
        {
        }

        protected override void VisitDocument(
            DocumentNode document,
            ImmutableStack<ISyntaxNode> path)
        {
            foreach (OperationDefinitionNode operation in document.Definitions
                .OfType<OperationDefinitionNode>())
            {
                VisitOperationDefinition(operation, path);
            }

            foreach (FragmentDefinitionNode fragment in document.Definitions
                .OfType<FragmentDefinitionNode>())
            {
                string fragmentName = fragment.Name.Value;
                if (_visitedFragments.Add(fragmentName))
                {
                    VisitFragmentDefinition(fragment, path);
                }
            }

            FindNonMergableFields();
        }

        protected override void VisitField(
            FieldNode field,
            IType type,
            ImmutableStack<ISyntaxNode> path)
        {
            if (!IntrospectionFields.TypeName.Equals(field.Name.Value))
            {
                if (TryGetSelectionSet(path, out SelectionSetNode selectionSet)
                    && _fieldSelectionSets.TryGetValue(selectionSet,
                        out List<FieldInfo> fields))
                {
                    fields.Add(new FieldInfo(type, field));
                }

                base.VisitField(field, type, path);
            }
        }

        protected override void VisitSelectionSet(
            SelectionSetNode selectionSet,
            IType type,
            ImmutableStack<ISyntaxNode> path)
        {
            if (!_fieldSelectionSets.ContainsKey(selectionSet))
            {
                _fieldSelectionSets.Add(selectionSet, new List<FieldInfo>());
            }
            base.VisitSelectionSet(selectionSet, type, path);
        }

        private bool TryGetSelectionSet(
            ImmutableStack<ISyntaxNode> path,
            out SelectionSetNode selectionSet)
        {
            SelectionSetNode root = null;
            ImmutableStack<ISyntaxNode> current = path;

            while (current.Any())
            {
                if (current.Peek() is SelectionSetNode set)
                {
                    root = set;
                    if (IsRelevantSelectionSet(current.Pop().Peek()))
                    {
                        selectionSet = set;
                        return true;
                    }
                }

                current = current.Pop();
            }

            selectionSet = root;
            return root != null;
        }

        private bool IsRelevantSelectionSet(ISyntaxNode syntaxNode)
        {
            return syntaxNode is FieldNode
                || syntaxNode is OperationDefinitionNode;
        }

        private void FindNonMergableFields()
        {
            foreach (KeyValuePair<SelectionSetNode, List<FieldInfo>> entry in
                _fieldSelectionSets)
            {
                foreach (IGrouping<string, FieldInfo> fieldGroup in
                    entry.Value.GroupBy(t => t.ResponseName))
                {
                    if (!CanFieldsInSetMerge(fieldGroup))
                    {
                        Errors.Add(new ValidationError(
                            "The query has non-mergable fields.",
                            fieldGroup.Select(t => t.Field)));
                    }
                }
            }
        }

        private bool CanFieldsInSetMerge(
            IGrouping<string, FieldInfo> fieldGroup)
        {
            if (fieldGroup.Count() > 1)
            {
                FieldInfo fieldA = fieldGroup.First();

                foreach (FieldInfo fieldB in fieldGroup.Skip(1))
                {
                    if (!SameResponseShape(fieldA, fieldB)
                        || !CanFieldsInSetMerge(fieldA, fieldB))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool CanFieldsInSetMerge(
           FieldInfo fieldA, FieldInfo fieldB)
        {
            if (fieldA.DeclaringType == fieldB.DeclaringType)
            {
                if (fieldA.Field.Name.Value
                    .Equals(fieldB.Field.Name.Value,
                        StringComparison.Ordinal)
                    && AreFieldArgumentsEqual(fieldA, fieldB))
                {
                    return true;
                }

                return false;
            }
            else
            {
                return true;
            }
        }

        private bool AreFieldArgumentsEqual(FieldInfo fieldA, FieldInfo fieldB)
        {
            if (fieldA.Field.Arguments.Count == fieldB.Field.Arguments.Count)
            {
                if (fieldA.Field.Arguments.Count == 0)
                {
                    return true;
                }

                Dictionary<string, ArgumentNode> argumentsOfB =
                    CreateArgumentLookup(fieldB.Field);

                foreach (ArgumentNode argumentA in fieldA.Field.Arguments)
                {
                    if (!argumentsOfB.TryGetValue(argumentA.Name.Value,
                        out ArgumentNode argumentB)
                        || !AreFieldArgumentsEqual(argumentA, argumentB))
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        private bool AreFieldArgumentsEqual(
            ArgumentNode argumentA,
            ArgumentNode argumentB)
        {
            return argumentA.Name.Value == argumentB.Name.Value
                && argumentA.Value.Equals(argumentB.Value);
        }

        private Dictionary<string, ArgumentNode> CreateArgumentLookup(
            FieldNode field)
        {
            var arguments = new Dictionary<string, ArgumentNode>();
            foreach (ArgumentNode argument in field.Arguments)
            {
                arguments[argument.Name.Value] = argument;
            }
            return arguments;
        }

        private bool SameResponseShape(FieldInfo fieldA, FieldInfo fieldB)
        {
            IType typeA = GetType(fieldA);
            IType typeB = GetType(fieldB);

            if (RemoveNonNullType(ref typeA, ref typeB)
                && RemoveListType(ref typeA, ref typeB)
                && RemoveNonNullType(ref typeA, ref typeB))
            {
                if ((typeA.IsScalarType() || typeB.IsScalarType())
                    || (typeA.IsEnumType() || typeB.IsEnumType()))
                {
                    return typeA == typeB;
                }

                if (typeA != null && typeB != null
                    && (!typeA.IsCompositeType() || !typeB.IsCompositeType()))
                {
                    return false;
                }

                return true;
            }

            return false;
        }

        private bool RemoveNonNullType(ref IType typeA, ref IType typeB)
        {
            if (typeA.IsNonNullType() || typeB.IsNonNullType())
            {
                if (!typeA.IsNonNullType() || !typeB.IsNonNullType())
                {
                    return false;
                }

                typeA = typeA.InnerType();
                typeB = typeB.InnerType();
            }

            return true;
        }

        private bool RemoveListType(ref IType typeA, ref IType typeB)
        {
            if (typeA.IsListType() || typeB.IsListType())
            {
                if (!typeA.IsListType() || !typeB.IsListType())
                {
                    return false;
                }

                typeA = typeA.InnerType();
                typeB = typeB.InnerType();
            }

            return true;
        }

        private IType GetType(FieldInfo fieldInfo)
        {
            if (fieldInfo.DeclaringType is IComplexOutputType c
                && c.Fields.TryGetField(fieldInfo.Field.Name.Value,
                    out IOutputField field))
            {
                return field.Type;
            }
            return null;
        }

        private readonly struct FieldInfo
        {
            public FieldInfo(IType declaringType, FieldNode field)
            {
                DeclaringType = declaringType
                    ?? throw new ArgumentNullException(nameof(declaringType));
                Field = field
                    ?? throw new ArgumentNullException(nameof(field));
                ResponseName = Field.Alias == null
                    ? Field.Name.Value
                    : Field.Alias.Value;
            }

            public string ResponseName { get; }
            public IType DeclaringType { get; }
            public FieldNode Field { get; }
        }
    }
}
