using System;
using System.Collections.Generic;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Validation.Properties;

namespace HotChocolate.Validation
{
    public sealed class DocumentValidatorContext : IDocumentValidatorContext
    {
        private static readonly FieldInfoListBufferPool _fieldInfoPool = new();
        private readonly List<FieldInfoListBuffer> _buffers = new() { new FieldInfoListBuffer() };

        private ISchema? _schema;
        private IOutputType? _nonNullString;

        public ISchema Schema
        {
            get
            {
                if (_schema is null)
                {
                    throw new InvalidOperationException(
                        Resources.DocumentValidatorContext_Context_Invalid_State);
                }
                return _schema;
            }
            set
            {
                _schema = value;
                NonNullString = new NonNullType(_schema.GetType<StringType>("String"));
            }
        }

        public IOutputType NonNullString
        {
            get
            {
                if (_nonNullString is null)
                {
                    throw new InvalidOperationException(
                        Resources.DocumentValidatorContext_Context_Invalid_State);
                }
                return _nonNullString;
            }
            private set => _nonNullString = value;
        }

        public IList<ISyntaxNode> Path { get; } = new List<ISyntaxNode>();

        public IList<SelectionSetNode> SelectionSets { get; } = new List<SelectionSetNode>();

        public IDictionary<SelectionSetNode, IList<FieldInfo>> FieldSets { get; } =
            new Dictionary<SelectionSetNode, IList<FieldInfo>>();

        public ISet<(FieldNode, FieldNode)> FieldTuples { get; } =
            new HashSet<(FieldNode, FieldNode)>();

        public ISet<string> VisitedFragments { get; } = new HashSet<string>();

        public IVariableValueCollection? VariableValues { get; set; }

        public IDictionary<string, VariableDefinitionNode> Variables { get; } =
            new Dictionary<string, VariableDefinitionNode>();

        public IDictionary<string, FragmentDefinitionNode> Fragments { get; } =
            new Dictionary<string, FragmentDefinitionNode>();

        public ISet<string> Used { get; } = new HashSet<string>();

        public ISet<string> Unused { get; } = new HashSet<string>();

        public ISet<string> Declared { get; } = new HashSet<string>();

        public ISet<string> Names { get; } = new HashSet<string>();

        public IList<IType> Types { get; } = new List<IType>();

        public IList<DirectiveType> Directives { get; } = new List<DirectiveType>();

        public IList<IOutputField> OutputFields { get; } = new List<IOutputField>();

        public IList<FieldNode> Fields { get; } = new List<FieldNode>();

        public IList<IInputField> InputFields { get; } = new List<IInputField>();

        public ICollection<IError> Errors { get; } = new List<IError>();

        public IList<object?> List { get; } = new List<object?>();

        public bool UnexpectedErrorsDetected { get; set; }

        public int Count { get; set; }

        public int Max { get; set; }

        public IDictionary<string, object?> ContextData { get; set; }

        public IList<FieldInfo> RentFieldInfoList()
        {
            FieldInfoListBuffer buffer = _buffers.Peek();

            if (!buffer.TryPop(out IList<FieldInfo>? list))
            {
                buffer = _fieldInfoPool.Get();
                _buffers.Push(buffer);
                list = buffer.Pop();
            }

            return list;
        }

        public void Clear()
        {
            ClearBuffers();

            _schema = null;
            _nonNullString = null;
            VariableValues = null;
            ContextData = default!;
            Path.Clear();
            SelectionSets.Clear();
            FieldSets.Clear();
            FieldTuples.Clear();
            VisitedFragments.Clear();
            Variables.Clear();
            Fragments.Clear();
            Used.Clear();
            Unused.Clear();
            Declared.Clear();
            Names.Clear();
            Types.Clear();
            Directives.Clear();
            OutputFields.Clear();
            Fields.Clear();
            InputFields.Clear();
            Errors.Clear();
            List.Clear();
            UnexpectedErrorsDetected = false;
            Count = 0;
            Max = 0;
        }

        private void ClearBuffers()
        {
            if (_buffers.Count > 1)
            {
                FieldInfoListBuffer buffer = _buffers.Pop();
                buffer.Clear();

                for (var i = 0; i < _buffers.Count; i++)
                {
                    _fieldInfoPool.Return(_buffers[i]);
                }

                _buffers.Clear();
                _buffers.Add(buffer);
            }
            else
            {
                _buffers[0].Clear();
            }
        }
    }
}
