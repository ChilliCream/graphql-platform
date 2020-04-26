using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    public interface IDocumentValidatorContext : ISyntaxVisitorContext
    {
        ISchema Schema { get; }

        IOutputType NonNullString { get; }

        IList<ISyntaxNode> Path { get; }

        IList<SelectionSetNode> SelectionSets { get; }

        IDictionary<SelectionSetNode, IList<FieldInfo>> FieldSets { get; }

        ISet<string> VisitedFragments { get; }

        IDictionary<string, object> VariableValues { get; }

        IDictionary<string, VariableDefinitionNode> Variables { get; }

        IDictionary<string, FragmentDefinitionNode> Fragments { get; }

        ISet<string> Used { get; }

        ISet<string> Unused { get; }

        ISet<string> Declared { get; }

        ISet<string> Names { get; }

        IList<IType> Types { get; }

        IList<DirectiveType> Directives { get; }

        IList<IOutputField> OutputFields { get; }

        IList<FieldNode> Fields { get; }

        IList<IInputField> InputFields { get; }

        ICollection<IError> Errors { get; }

        int Count { get; set; }

        int Max { get; set; }

        bool UnexpectedErrorsDetected { get; set; }

        IDictionary<string, object> ContextData { get; }

        IList<FieldInfo> RentFieldInfoList();
    }
}
