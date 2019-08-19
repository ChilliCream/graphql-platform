using System.IO;
using System;
using System.Collections.Generic;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using System.Linq;
using System.Threading.Tasks;

namespace StrawberryShake.Generators
{
    public class DocumentVisitor
        : ISyntaxNodeVisitor<OperationDefinitionNode>
        , ISyntaxNodeVisitor<FieldNode>
        , ISyntaxNodeVisitor<InlineFragmentNode>
        , ISyntaxNodeVisitor<FragmentDefinitionNode>
        , ISyntaxNodeVisitor<SelectionSetNode>
    {
        private readonly InterfaceGenerator _generator = new InterfaceGenerator();
        private readonly Stack<IType> _types = new Stack<IType>();
        private readonly HashSet<string> _typeNames = new HashSet<string>();
        private readonly Stack<List<SelectionSetNode>> _grouped = new Stack<List<SelectionSetNode>>();
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
            string name = null;

            if (node.Selections.Count == 1
                && node.Selections[0] is FragmentSpreadNode f)
            {
                name = f.Name.Value;

                // generate interfac
            }
            else
            {
                name = _types.Peek().NamedType().Name;
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

                string fileName = NameUtils.GetInterfaceName(name) + ".cs";
                INamedType type = _types.Peek().NamedType();

                _fileHandler.WriteTo(fileName, async stream =>
                {
                    using (var sw = new StreamWriter(stream))
                    {
                        using (var cw = new CodeWriter(sw))
                        {
                            await _generator.WriteAsync(
                                cw,
                                _schema,
                                type,
                                node,
                                node.Selections.OfType<FieldNode>(),
                                name);
                        }
                    }
                });
            }
            return VisitorAction.Continue;
        }
    }

    public interface IFileHandler
    {
        void WriteTo(string fileName, Func<Stream, Task> write);
    }
}
