using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    public interface IDocumentValidatorContext : ISyntaxVisitorContext
    {
        ISchema Schema { get; }

        Stack<ISyntaxNode> Path { get; }

        IDictionary<string, FragmentDefinitionNode> Fragments { get; }

        ISet<string> UsedVariables { get; }

        ISet<string> UnusedVariables { get; }

        ISet<string> DeclaredVariables { get; }

        Stack<IType> Types { get; }

        Stack<DirectiveType> Directives { get; }

        ICollection<IError> Errors { get; }
    }
}
