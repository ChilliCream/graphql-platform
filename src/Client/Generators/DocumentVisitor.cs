using System.IO;
using System;
using System.Collections.Generic;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly HashSet<string> _typeNames = new HashSet<string>();
        private readonly Stack<List<SelectionSetNode>> _grouped =
            new Stack<List<SelectionSetNode>>();
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
                _field.Push(new FieldInfo(field, node));

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
                && (IsRemoved(n) || n is FragmentSpreadNode))
            {
                return VisitorAction.Continue;
            }

            string name = GetName(node, ancestors);

            var descriptor = new InterfaceCodeDescriptor(
                _types.Peek().NamedType(),
                NameUtils.GetInterfaceName(name),
                node.Selections.OfType<FieldNode>().ToList());

            GenerateModelInterface(descriptor);
            GenerateModelClass(
                NameUtils.GetClassName(name),
                new[] { descriptor });
            return VisitorAction.Continue;
        }

        private string GetName(
            SelectionSetNode node,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            string name = null;

            int last = ancestors.Count - 1;
            for (int i = last; i >= 0; i--)
            {
                if (ancestors[i] is NamedSyntaxNode n
                    && IsRemoved(n))
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

            if (!_typeNames.Add(name))
            {
                for (int i = 0; i < int.MaxValue; i++)
                {
                    string n = name + i;
                    if (_typeNames.Add(n))
                    {
                        name = n;
                        break;
                    }
                }
            }

            return name;
        }

        private bool IsRemoved(NamedSyntaxNode node)
        {
            return node.Directives.Any(t =>
                t.Name.Value.Equals(
                    "remove",
                    StringComparison.InvariantCulture));
        }

        private bool TryGetTypeName(
            WithDirectives withDirectives,
            out string typeName)
        {
            DirectiveNode directive = withDirectives.Directives.FirstOrDefault(t =>
                t.Name.Value.Equals("type", StringComparison.InvariantCulture));

            if (directive is null)
            {
                typeName = null;
                return false;
            }

            typeName = (string)directive.Arguments.Single(a =>
                a.Name.Value.Equals("name", StringComparison.InvariantCulture)).Value.Value;
            return true;
        }

        private void GenerateModelInterface(
            InterfaceCodeDescriptor descriptor)
        {
            string fileName = descriptor.Name + ".cs";

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
            IReadOnlyList<InterfaceCodeDescriptor> implements)
        {
            string fileName = name + ".cs";
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
    }


    public interface IFileHandler
    {
        void WriteTo(string fileName, Func<Stream, Task> write);
    }

    public class InterfaceCodeDescriptor
    {
        public InterfaceCodeDescriptor(
            INamedType type,
            string name,
            IReadOnlyList<FieldNode> fields)
        {
            Type = type;
            Name = name;
            Fields = fields;
        }

        public INamedType Type { get; }
        public string Name { get; }
        public IReadOnlyList<FieldNode> Fields { get; }
    }
}
