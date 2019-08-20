using System.IO;
using System;
using System.Collections.Generic;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using System.Linq;
using WithDirectives = HotChocolate.Language.IHasDirectives;

namespace StrawberryShake.Generators
{
    public class DocumentVisitor
        : ISyntaxNodeVisitor<OperationDefinitionNode>
        , ISyntaxNodeVisitor<FieldNode>
        , ISyntaxNodeVisitor<InlineFragmentNode>
        , ISyntaxNodeVisitor<FragmentDefinitionNode>
        , ISyntaxNodeVisitor<SelectionSetNode>
        , ITypeLookup
    {
        private readonly Stack<IType> _types = new Stack<IType>();
        private readonly Dictionary<SelectionSetNode, string> _typeNames =
            new Dictionary<SelectionSetNode, string>();
        private readonly Stack<List<SelectionSetNode>> _grouped =
            new Stack<List<SelectionSetNode>>();
        private readonly Dictionary<FieldNode, IType> _fieldTypes =
            new Dictionary<FieldNode, IType>();
        private readonly Dictionary<FieldNode, SelectionSetNode> _fieldSelectionSets =
            new Dictionary<FieldNode, SelectionSetNode>();
        private readonly List<IClassDescriptor> _classDescriptors = new List<IClassDescriptor>();
        private readonly ISchema _schema;
        private readonly IFileHandler _fileHandler;

        public DocumentVisitor(ISchema schema, IFileHandler fileHandler)
        {
            _schema = schema;
            _fileHandler = fileHandler;
        }

        public VisitorAction Enter(
            OperationDefinitionNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            ObjectType type = _schema.GetOperationType(node.Operation);

            if (type is null)
            {
                throw new InvalidOperationException(
                    "The specified operation type does not exist.");
            }

            _types.Push(type);

            return VisitorAction.Continue;
        }

        public VisitorAction Leave(
            OperationDefinitionNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            _types.Pop();

            return VisitorAction.Continue;
        }

        public VisitorAction Enter(
           FieldNode node,
           ISyntaxNode parent,
           IReadOnlyList<object> path,
           IReadOnlyList<ISyntaxNode> ancestors)
        {
            if (_types.Peek().NamedType() is IComplexOutputType complexType
                && complexType.Fields.TryGetField(node.Name.Value, out IOutputField field))
            {
                _types.Push(field.Type);

                return VisitorAction.Continue;
            }

            return VisitorAction.Break;
        }


        public VisitorAction Leave(
            FieldNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            _types.Pop();

            return VisitorAction.Continue;
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
                _types.Push(type);
                return VisitorAction.Continue;
            }

            return VisitorAction.Break;
        }

        public VisitorAction Leave(
            InlineFragmentNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            _types.Pop();
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
                _types.Push(type);
                return VisitorAction.Continue;
            }

            return VisitorAction.Break;
        }

        public VisitorAction Leave(
            FragmentDefinitionNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            _types.Pop();
            return VisitorAction.Continue;
        }

        public VisitorAction Enter(
            SelectionSetNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            return VisitorAction.Continue;
        }

        public VisitorAction Leave(
            SelectionSetNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            if (node.Selections.Count == 1
               && node.Selections[0] is NamedSyntaxNode n
               && (Spread(n) || n is FragmentSpreadNode))
            {
                return VisitorAction.Continue;
            }

            string typeName = GetName(node, ancestors);
            PushType(node, ancestors, typeName);
            InterfaceDescriptor descriptor =
                CreateInterfaceDescriptor(typeName, node);

            return VisitorAction.Continue;
        }

        private string GetName(
            SelectionSetNode node,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            if (_typeNames.TryGetValue(node, out string name))
            {
                return name;
            }

            int last = ancestors.Count - 1;
            for (int i = last; i >= 0; i--)
            {
                if (ancestors[i] is NamedSyntaxNode n
                    && Spread(n))
                {
                    i--;
                }
                else
                {
                    if (ancestors[i] is FragmentDefinitionNode f)
                    {
                        name = f.Name.Value;
                    }
                    else if (ancestors[i] is WithDirectives wd
                        && TryGetTypeName(wd, out string typeName))
                    {
                        name = typeName;
                    }
                    else
                    {
                        name = _types.Peek().NamedType().Name;
                    }
                    break;
                }
            }

            if (_typeNames.ContainsValue(name))
            {
                for (int i = 0; i < int.MaxValue; i++)
                {
                    string n = name + i;
                    if (!_typeNames.ContainsValue(n))
                    {
                        name = n;
                        break;
                    }
                }
            }

            _typeNames[node] = name;
            return name;
        }

        private bool Spread(NamedSyntaxNode node)
        {
            return node.Directives.Any(t =>
                t.Name.Value.Equals(
                    GeneratorDirectives.Spread,
                    StringComparison.InvariantCulture));
        }

        private bool TryGetTypeName(
            WithDirectives withDirectives,
            out string typeName)
        {
            DirectiveNode directive =
                withDirectives.Directives.FirstOrDefault(t =>
                    t.Name.Value.EqualsOrdinal(GeneratorDirectives.Type));

            if (directive is null)
            {
                typeName = null;
                return false;
            }

            typeName = (string)directive.Arguments.Single(a =>
                a.Name.Value.EqualsOrdinal("name")).Value.Value;
            return true;
        }

        private void PushType(
            SelectionSetNode node,
            IReadOnlyList<ISyntaxNode> ancestors,
            string typeName)
        {
            var types = new Stack<IType>(_types.Reverse());
            types.Pop();

            int last = ancestors.Count - 1;
            for (int i = last; i >= 0; i--)
            {
                if (ancestors[i] is SelectionSetNode n)
                {
                    _typeNames[n] = typeName;
                    break;
                }
            }
        }

        private InterfaceDescriptor CreateInterfaceDescriptor(
            string typeName,
            SelectionSetNode selectionSet)
        {
            if (_types.Peek().NamedType() is IComplexOutputType type)
            {
                var fields = new List<IFieldDescriptor>();
                foreach (FieldNode selection in selectionSet.Selections.OfType<FieldNode>())
                {
                    IOutputField field = type.Fields[selection.Name.Value];
                    if (!_fieldTypes.TryGetValue(selection, out IType fieldType))
                    {
                        fieldType = field.Type;
                    }

                    fields.Add(new FieldSelection(field, selection, fieldType));
                }

                return new InterfaceDescriptor(
                    type,
                    NameUtils.GetInterfaceName(typeName),
                    fields);
            }

            throw new InvalidOperationException();
        }

        public string GetTypeName(FieldNode field, IType fieldType, bool readOnly)
        {
            if (fieldType.NamedType().IsScalarType())
            {
                return "string";
            }
            else if (fieldType.NamedType().IsEnumType())
            {
                return "string";
            }
            else if (_fieldSelectionSets.TryGetValue(field, out SelectionSetNode selectionSet)
                && _typeNames.TryGetValue(selectionSet, out string typeName))
            {
                return BuildType(fieldType, typeName, readOnly);
            }

            throw new NotSupportedException();
        }

        private static string BuildType(IType type, string typeName, bool readOnly)
        {
            if (type is NonNullType nnt)
            {
                return BuildType(nnt.Type, typeName, readOnly);
            }
            else if (type is ListType lt)
            {
                return readOnly
                    ? $"IReadOnlyList<{BuildType(lt.ElementType, typeName, readOnly)}>"
                    : $"List<{BuildType(lt.ElementType, typeName, readOnly)}>";
            }
            else
            {
                return typeName;
            }
        }
    }
}
