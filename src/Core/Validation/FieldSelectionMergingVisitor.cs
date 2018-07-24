using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    internal sealed class FieldSelectionMergingVisitor
        : QueryVisitorErrorBase
    {
        private Dictionary<SelectionSetNode, List<FieldInfo>> _fieldSelectionSets =
            new Dictionary<SelectionSetNode, List<FieldInfo>>();

        public FieldSelectionMergingVisitor(ISchema schema)
            : base(schema)
        {
        }

        public override void VisitDocument(DocumentNode document)
        {
            base.VisitDocument(document);
            FindNonMergableFields();
        }

        protected override void VisitField(
            FieldNode field,
            Types.IType type,
            ImmutableStack<ISyntaxNode> path)
        {
            if (TryGetSelectionSet(path, out SelectionSetNode selectionSet)
                && _fieldSelectionSets.TryGetValue(selectionSet,
                    out List<FieldInfo> fields))
            {
                fields.Add(new FieldInfo(type, field));
            }

            base.VisitField(field, type, path);
        }

        protected override void VisitSelectionSet(
            SelectionSetNode selectionSet,
            IType type,
            ImmutableStack<ISyntaxNode> path)
        {
            _fieldSelectionSets.Add(selectionSet, new List<FieldInfo>());
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
                    if (SameResponseShape(fieldA, fieldB))
                    {
                        // TODO : we have to check if the declaring types are different if those field belong to fragments
                        // if (fieldA.DeclaringType == fieldB.DeclaringType)
                        // {
                        if (fieldA.Field.Name.Value
                            .EqualsOrdinal(fieldB.Field.Name.Value)
                            && AreFieldArgumentsEqual(fieldA, fieldB))
                        {
                            return true;
                        }
                        // }
                        // else
                        // {

                        // }
                    }
                }

                return false;
            }

            return true;
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

                if (!typeA.IsCompositeType() || !typeB.IsCompositeType())
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
            public FieldInfo(Types.IType declaringType, FieldNode field)
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
            public Types.IType DeclaringType { get; }
            public FieldNode Field { get; }
        }
    }
}
