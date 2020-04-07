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

        ISet<string> VisitedFragments { get; }

        IDictionary<string, VariableDefinitionNode> Variables { get; }

        IDictionary<string, FragmentDefinitionNode> Fragments { get; }

        ISet<string> Used { get; }

        ISet<string> Unused { get; }

        ISet<string> Declared { get; }

        ISet<string> Names { get; }

        IList<IType> Types { get; }

        IList<DirectiveType> Directives { get; }

        IList<IOutputField> OutputFields { get; }

        IList<IInputField> InputFields { get; }

        ICollection<IError> Errors { get; }

        bool UnexpectedErrorsDetected { get; set; }
    }
}
