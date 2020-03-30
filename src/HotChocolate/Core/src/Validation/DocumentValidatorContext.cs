using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

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

        public IList<ISyntaxNode> Path { get; } = new List<ISyntaxNode>();

        public IDictionary<string, VariableDefinitionNode> Variables { get; } =
            new Dictionary<string, VariableDefinitionNode>();

        public IDictionary<string, FragmentDefinitionNode> Fragments { get; } =
            new Dictionary<string, FragmentDefinitionNode>();

        public ISet<string> UsedVariables { get; } = new HashSet<string>();

        public ISet<string> UnusedVariables { get; } = new HashSet<string>();

        public ISet<string> DeclaredVariables { get; } = new HashSet<string>();

        public IList<IType> Types { get; } = new List<IType>();

        public IList<DirectiveType> Directives { get; } = new List<DirectiveType>();

        public IList<IOutputField> OutputFields { get; } = new List<IOutputField>();

        public IList<IInputField> InputFields { get; } = new List<IInputField>();

        public ICollection<IError> Errors { get; } = new List<IError>();

        public bool IsInError { get; set; }

        public void Clear()
        {
            _schema = null;
            Path.Clear();
            Variables.Clear();
            Fragments.Clear();
            UsedVariables.Clear();
            UnusedVariables.Clear();
            DeclaredVariables.Clear();
            Types.Clear();
            Directives.Clear();
            OutputFields.Clear();
            InputFields.Clear();
            Errors.Clear();
            IsInError = false;
        }
    }
}
