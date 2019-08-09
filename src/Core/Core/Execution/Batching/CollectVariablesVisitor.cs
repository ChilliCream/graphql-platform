using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types;

namespace HotChocolate.Execution.Batching
{
    internal class CollectVariablesVisitor
        : ISyntaxNodeVisitor<OperationDefinitionNode>
        , ISyntaxNodeVisitor<FieldNode>
        , ISyntaxNodeVisitor<ArgumentNode>
        , ISyntaxNodeVisitor<ObjectFieldNode>
        , ISyntaxNodeVisitor<VariableNode>
        , ISyntaxNodeVisitor<InlineFragmentNode>
        , ISyntaxNodeVisitor<FragmentSpreadNode>
        , ISyntaxNodeVisitor<FragmentDefinitionNode>
    {
        private readonly Dictionary<string, VariableDefinitionNode> _variables =
            new Dictionary<string, VariableDefinitionNode>();
        private readonly HashSet<string> _declared = new HashSet<string>();
        private readonly Stack<IType> _type = new Stack<IType>();
        private readonly Stack<IOutputField> _field =
            new Stack<IOutputField>();
        private readonly Stack<VisitorAction> _action =
            new Stack<VisitorAction>();
        private readonly HashSet<string> _touchedFragments =
            new HashSet<string>();
        private readonly ISchema _schema;

        public CollectVariablesVisitor(ISchema schema)
        {
            _schema = schema ?? throw new ArgumentNullException(nameof(schema));
        }

        public IReadOnlyCollection<VariableDefinitionNode>
            VariableDeclarations =>
                _variables.Values;
        public IReadOnlyCollection<string> TouchedFragments =>
            _touchedFragments;

        public VisitorAction Enter(
            OperationDefinitionNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            _variables.Clear();
            _field.Clear();
            _type.Clear();
            _action.Clear();
            _touchedFragments.Clear();

            ObjectType type = _schema.GetOperationType(node.Operation);
            _type.Push(type);

            for (var i = 0; i < node.VariableDefinitions.Count; i++)
            {
                _declared.Add(
                    node.VariableDefinitions[i].Variable.Name.Value);
            }

            return VisitorAction.Continue;
        }

        public VisitorAction Leave(
            OperationDefinitionNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            _type.Pop();
            return VisitorAction.Continue;
        }

        public VisitorAction Enter(
            FieldNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            if (_type.Peek().NamedType() is IComplexOutputType complexType
                && complexType.Fields.TryGetField(
                    node.Name.Value, out IOutputField field))
            {
                _field.Push(field);
                _type.Push(field.Type);
                _action.Push(VisitorAction.Continue);
                return VisitorAction.Continue;
            }

            _action.Push(VisitorAction.Skip);
            return VisitorAction.Skip;
        }

        public VisitorAction Leave(
            FieldNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            if (_action.Pop() == VisitorAction.Continue)
            {
                _type.Pop();
                _field.Pop();
            }
            return VisitorAction.Continue;
        }

        public VisitorAction Enter(
            ArgumentNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            if (_field.Peek().Arguments.TryGetField(
                node.Name.Value,
                out IInputField field))
            {
                _type.Push(field.Type);
                _action.Push(VisitorAction.Continue);
                return VisitorAction.Continue;
            }

            _action.Push(VisitorAction.Skip);
            return VisitorAction.Skip;
        }

        public VisitorAction Leave(
            ArgumentNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            if (_action.Pop() == VisitorAction.Continue)
            {
                _type.Pop();
            }
            return VisitorAction.Continue;
        }

        public VisitorAction Enter(
            ObjectFieldNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            if (_type.Peek().NamedType() is InputObjectType inputObject
                && inputObject.Fields.TryGetField(
                    node.Name.Value,
                    out IInputField field))
            {
                _type.Push(field.Type);
                _action.Push(VisitorAction.Continue);
                return VisitorAction.Continue;
            }

            _action.Push(VisitorAction.Skip);
            return VisitorAction.Skip;
        }

        public VisitorAction Leave(
            ObjectFieldNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            if (_action.Pop() == VisitorAction.Continue)
            {
                _type.Pop();
            }
            return VisitorAction.Continue;
        }

        public VisitorAction Enter(
            VariableNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            if (!_declared.Contains(node.Name.Value))
            {
                IType type = _type.Peek();

                if (_variables.TryGetValue(
                    node.Name.Value,
                    out VariableDefinitionNode d))
                {
                    if (type.IsNonNullType()
                        && d.Type is INullableTypeNode nullable)
                    {
                        _variables[node.Name.Value] =
                            d.WithType(new NonNullTypeNode(nullable));
                    }
                }
                else
                {
                    if (type.NamedType() is IComplexOutputType complexType)
                    {
                        Type clrType = complexType.ToClrType();
                        InputObjectType inputType =
                            _schema.Types.OfType<InputObjectType>()
                                .First(t => t.ClrType == clrType);

                        if (inputType == null)
                        {
                            throw new QueryException(ErrorBuilder.New()
                                .SetMessage(CoreResources.BatchColVars_NoCompatibleType)
                                .SetCode(BatchingErrorCodes.AutoMapVarError)
                                .SetPath(path)
                                .AddLocation(node)
                                .Build());
                        }

                        d = new VariableDefinitionNode
                        (
                            null,
                            node,
                            type.ToTypeNode(inputType),
                            null,
                            Array.Empty<DirectiveNode>()
                        );
                    }
                    else
                    {
                        d = new VariableDefinitionNode
                        (
                            null,
                            node,
                            type.ToTypeNode(),
                            null,
                            Array.Empty<DirectiveNode>()
                        );
                    }

                    _variables[node.Name.Value] = d;
                }
            }

            return VisitorAction.Continue;
        }

        public VisitorAction Leave(
            VariableNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            return VisitorAction.Skip;
        }

        public VisitorAction Enter(
            InlineFragmentNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            if (_schema.TryGetType(
                node.TypeCondition.Name.Value,
                out INamedType type))
            {
                _type.Push(type);
                _action.Push(VisitorAction.Continue);
                return VisitorAction.Continue;
            }

            _action.Push(VisitorAction.Skip);
            return VisitorAction.Skip;
        }

        public VisitorAction Leave(
            InlineFragmentNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            if (_action.Pop() == VisitorAction.Continue)
            {
                _type.Pop();
            }
            return VisitorAction.Continue;
        }

        public VisitorAction Enter(
            FragmentDefinitionNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            if (_schema.TryGetType(
                node.TypeCondition.Name.Value,
                out INamedType type))
            {
                _touchedFragments.Add(node.Name.Value);
                _type.Push(type);
                _action.Push(VisitorAction.Continue);
                return VisitorAction.Continue;
            }

            _action.Push(VisitorAction.Skip);
            return VisitorAction.Skip;
        }

        public VisitorAction Leave(
            FragmentDefinitionNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            if (_action.Pop() == VisitorAction.Continue)
            {
                _type.Pop();
            }
            return VisitorAction.Continue;
        }

        public VisitorAction Enter(
            FragmentSpreadNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            return VisitorAction.Continue;
        }

        public VisitorAction Leave(
            FragmentSpreadNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            return VisitorAction.Continue;
        }
    }
}
