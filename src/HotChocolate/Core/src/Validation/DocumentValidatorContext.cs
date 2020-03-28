using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Validation
{
    public sealed class DocumentValidatorContext : IDocumentValidatorContext
    {
        private ISchema? _schema;

        public ISchema Schema
        {
            get
            {
                if (_schema is null)
                {
                    // TODO : resources
                    throw new InvalidOperationException(
                        "The context has an invalid state and is missing the schema.");
                }
                return _schema;
            }
            set
            {
                _schema = value;
            }
        }

        public Stack<ISyntaxNode> Path { get; } = new Stack<ISyntaxNode>();

        public IDictionary<string, FragmentDefinitionNode> Fragments { get; } =
            new Dictionary<string, FragmentDefinitionNode>();

        public ISet<string> UsedVariables { get; } = new HashSet<string>();

        public ISet<string> UnusedVariables { get; } = new HashSet<string>();

        public ISet<string> DeclaredVariables { get; } = new HashSet<string>();

        public ICollection<IError> Errors { get; } = new List<IError>();

        public void Clear()
        {
            _schema = null;
            Path.Clear();
            Fragments.Clear();
            UsedVariables.Clear();
            UnusedVariables.Clear();
            DeclaredVariables.Clear();
            Errors.Clear();
        }
    }
}
