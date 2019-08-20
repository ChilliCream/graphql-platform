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
    {
        private readonly ModelInterfaceGenerator _modelInterfaceGenerator =
            new ModelInterfaceGenerator();
        private readonly ModelClassGenerator _modelClassGenerator =
            new ModelClassGenerator();
        private readonly Stack<IType> _types = new Stack<IType>();
        private readonly Dictionary<SelectionSetNode, string> _typeNames =
            new Dictionary<SelectionSetNode, string>();
        private readonly Stack<List<SelectionSetNode>> _grouped =
            new Stack<List<SelectionSetNode>>();
        private readonly Dictionary<FieldNode, IType> _fieldTypes =
            new Dictionary<FieldNode, IType>();
        private readonly Dictionary<FieldNode, SelectionSetNode> _fieldSelectionSets =
            new Dictionary<FieldNode, SelectionSetNode>();
        private readonly Stack<FieldInfo> _field = new Stack<FieldInfo>();
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
                _field.Push(new FieldInfo(field, node, field.Type));

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
            _field.Pop();



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

            ChangeType(node, ancestors, _types.Peek());
            string typeName = GetName(node, ancestors);
            InterfaceDescriptor descriptor =
                CreateInterfaceDescriptor(typeName, node);

            GenerateModelInterface(descriptor);
            GenerateModelClass(
                NameUtils.GetClassName(typeName),
                new[] { descriptor });
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

        private void ChangeType(
            SelectionSetNode node,
            IReadOnlyList<ISyntaxNode> ancestors,
            IType type)
        {
            var types = new Stack<IType>(_types.Reverse());
            types.Pop();

            int last = ancestors.Count - 1;
            for (int i = last; i >= 0; i--)
            {
                if (ancestors[i] is NamedSyntaxNode n
                    && Spread(n))
                {
                    i--;
                    types.Pop();
                }
                else
                {
                    if (ancestors[i] is FieldNode field)
                    {
                        _fieldTypes[field] = type;
                        _fieldSelectionSets[field] = node;
                    }
                    else if (ancestors[i] is FragmentDefinitionNode)
                    {
                        if (ancestors[i - 2] is SelectionSetNode selectionSet
                            && selectionSet.Selections.Count == 1
                            && ancestors[i - 3] is FieldNode field2)
                        {
                            _fieldTypes[field2] = type;
                            _fieldSelectionSets[field2] = node;
                        }
                        else
                        {
                            // TODO : resources
                            // TODO : exception type
                            throw new InvalidOperationException();
                        }
                    }

                    break;
                }
            }

        }

        private void GenerateModelInterface(
            InterfaceDescriptor descriptor)
        {
            string fileName = descriptor.Name + FileExtensions.CSharp;

            _fileHandler.WriteTo(fileName, async stream =>
            {
                var sw = new StreamWriter(stream);
                var cw = new CodeWriter(sw);

                await _modelInterfaceGenerator.WriteAsync(
                    cw,
                    _schema,
                    descriptor);

                await cw.FlushAsync();
                await sw.FlushAsync();
            });
        }

        private void GenerateModelClass(
            string name,
            IReadOnlyList<InterfaceDescriptor> implements)
        {
            string fileName = name + FileExtensions.CSharp;
            INamedType type = _types.Peek().NamedType();

            _fileHandler.WriteTo(fileName, async stream =>
            {
                var sw = new StreamWriter(stream);
                var cw = new CodeWriter(sw);

                await _modelClassGenerator.WriteAsync(
                    cw,
                    _schema,
                    type,
                    name,
                    implements);

                await cw.FlushAsync();
                await sw.FlushAsync();
            });
        }

        private InterfaceDescriptor CreateInterfaceDescriptor(
            string typeName,
            SelectionSetNode selectionSet)
        {
            if (_types.Peek().NamedType() is IComplexOutputType type)
            {
                var fields = new List<FieldInfo>();
                foreach (FieldNode selection in selectionSet.Selections.OfType<FieldNode>())
                {
                    IOutputField field = type.Fields[selection.Name.Value];
                    if (!_fieldTypes.TryGetValue(selection, out IType fieldType))
                    {
                        fieldType = field.Type;
                    }

                    fields.Add(new FieldInfo(field, selection, fieldType));
                }

                return new InterfaceDescriptor(
                    type,
                    NameUtils.GetInterfaceName(typeName),
                    fields);
            }

            throw new InvalidOperationException();
        }
    }
}
