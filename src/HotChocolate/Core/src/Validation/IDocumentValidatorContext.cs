using System.Collections.Generic;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation;

/// <summary>
/// This interface represents the document validation context that can
/// be used by validation visitors to build up state.
/// </summary>
public interface IDocumentValidatorContext
{
    /// <summary>
    /// Gets the schema on which the validation is executed.
    /// </summary>
    ISchema Schema { get; }

    /// <summary>
    /// Gets the unique document identifier.
    /// </summary>
    OperationDocumentId DocumentId { get; }

    /// <summary>
    /// Gets the current operation type.
    /// </summary>
    OperationType? OperationType { get; set; }

    /// <summary>
    /// Gets a non-null string type.
    /// </summary>
    IOutputType NonNullString { get; }

    /// <summary>
    /// Specifies the max allowed validation errors.
    /// </summary>
    int MaxAllowedErrors { get; }

    /// <summary>
    /// The current visitation path of syntax nodes.
    /// </summary>
    IList<ISyntaxNode> Path { get; }

    /// <summary>
    /// The current visitation path of selection sets.
    /// </summary>
    IList<SelectionSetNode> SelectionSets { get; }

    /// <summary>
    /// A dictionary to store field infos per selection set.
    /// </summary>
    IDictionary<SelectionSetNode, IList<FieldInfo>> FieldSets { get; }

    /// <summary>
    /// A set of field tuples.
    /// </summary>
    ISet<(FieldNode, FieldNode)> FieldTuples { get; }

    /// <summary>
    /// Gets a set of already visited fragment names.
    /// </summary>
    ISet<string> VisitedFragments { get; }

    /// <summary>
    /// Gets the raw variable values.
    /// </summary>
    IVariableValueCollection? VariableValues { get; }

    /// <summary>
    /// Gets a map exposing the variable definitions by name.
    /// </summary>
    IDictionary<string, VariableDefinitionNode> Variables { get; }

    /// <summary>
    /// Gets a map exposing the fragment definitions by name.
    /// </summary>
    IDictionary<string, FragmentDefinitionNode> Fragments { get; }

    /// <summary>
    /// Gets a set to track usages of arbitrary names.
    /// </summary>
    ISet<string> Used { get; }

    /// <summary>
    /// Gets a set to track which names are not used.
    /// </summary>
    ISet<string> Unused { get; }

    /// <summary>
    /// Gets a set which names are declared.
    /// </summary>
    ISet<string> Declared { get; }

    /// <summary>
    /// Gets a set to track which names.
    /// </summary>
    ISet<string> Names { get; }

    /// <summary>
    /// The current visitation path of types.
    /// </summary>
    IList<IType> Types { get; }

    /// <summary>
    /// The current visitation path of directive types.
    /// </summary>
    IList<DirectiveType> Directives { get; }

    /// <summary>
    /// The current visitation path of output fields.
    /// </summary>
    IList<IOutputField> OutputFields { get; }

    /// <summary>
    /// The current visitation path of selections.
    /// </summary>
    IList<FieldNode> Fields { get; }

    /// <summary>
    /// The current visitation path of input fields.
    /// </summary>
    IList<IInputField> InputFields { get; }

    /// <summary>
    /// A list to track validation errors that occurred during the visitation.
    /// </summary>
    IReadOnlyCollection<IError> Errors { get; }

    /// <summary>
    /// Gets ors sets a single counter.
    /// </summary>
    int Count { get; set; }

    /// <summary>
    /// Gets or sets a single max value counter.
    /// </summary>
    int Max { get; set; }

    /// <summary>
    /// Gets or sets a value representing an allowed limit.
    /// </summary>
    int Allowed { get; set; }

    /// <summary>
    /// Gets a list of objects that can be used by validation rules.
    /// </summary>
    IList<object?> List { get; }

    /// <summary>
    /// Defines that a visitation has found an unexpected error
    /// that is no concern of the current validation rule.
    /// If no other error is found by any validation this will
    /// lead to an unexpected validation error.
    /// </summary>
    bool UnexpectedErrorsDetected { get; set; }

    /// <summary>
    /// A map to store arbitrary visitor data.
    /// </summary>
    IDictionary<string, object?> ContextData { get; }

    /// <summary>
    /// When processing field merging this list holds the field pairs that are processed.
    /// </summary>
    List<FieldInfoPair> CurrentFieldPairs  { get; }

    /// <summary>
    /// When processing field merging this list holds the field pairs that are processed next.
    /// </summary>
    List<FieldInfoPair> NextFieldPairs  { get; }

    /// <summary>
    /// When processing field merging this set represents the already processed field pairs.
    /// </summary>
    HashSet<FieldInfoPair> ProcessedFieldPairs  { get; }

    /// <summary>
    /// Rents a list of field infos.
    /// </summary>
    IList<FieldInfo> RentFieldInfoList();

    /// <summary>
    /// Reports an error.
    /// </summary>
    /// <param name="error">
    /// The validation error that shall be reported.
    /// </param>
    void ReportError(IError error);
}
