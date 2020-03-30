using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    public interface IDocumentValidatorContext : ISyntaxVisitorContext
    {
        ISchema Schema { get; }

        IList<ISyntaxNode> Path { get; }

        IDictionary<string, VariableDefinitionNode> Variables { get; }

        IDictionary<string, FragmentDefinitionNode> Fragments { get; }

        ISet<string> UsedVariables { get; }

        ISet<string> UnusedVariables { get; }

        ISet<string> DeclaredVariables { get; }

        ISet<string> Names { get; }

        IList<IType> Types { get; }

        IList<DirectiveType> Directives { get; }

        IList<IOutputField> OutputFields { get; }

        IList<IInputField> InputFields { get; }

        ICollection<IError> Errors { get; }

        /// <summary>
        /// The visitor was unable to resolver types specified in the query.
        /// </summary>
        bool IsInError { get; set; }
    }
}
