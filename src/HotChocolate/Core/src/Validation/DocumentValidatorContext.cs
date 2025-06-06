using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Validation.Properties;

namespace HotChocolate.Validation;

/// <summary>
/// This interface represents the document validation context that can
/// be used by validation visitors to build up state.
/// </summary>
public sealed class DocumentValidatorContext : IFeatureProvider
{
    private readonly List<IError> _errors = [];
    private readonly PooledFeatureCollection _features;
    private ISchemaDefinition? _schema;
    private int _maxAllowedErrors;

    /// <summary>
    /// Initializes a new instance of <see cref="DocumentValidatorContext"/>.
    /// </summary>
    public DocumentValidatorContext()
    {
        _features = new PooledFeatureCollection(this);
        _features.Set(this);
    }

    /// <summary>
    /// Gets the schema on which the validation is executed.
    /// </summary>
    public ISchemaDefinition Schema
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
    }

    /// <summary>
    /// Gets the unique document identifier.
    /// </summary>
    public OperationDocumentId DocumentId { get; private set; }

    /// <summary>
    /// Gets the document that is being validated.
    /// </summary>
    public DocumentNode Document { get; private set; } = null!;

    /// <summary>
    /// The current visitation path of syntax nodes.
    /// </summary>
    public List<ISyntaxNode> Path { get; } = [];

    /// <summary>
    /// The current visitation path of selection sets.
    /// </summary>
    public List<SelectionSetNode> SelectionSets { get; } = [];

    /// <summary>
    /// Gets the fragment context used to track the fragments that are visited
    /// during the visitation of a document.
    /// </summary>
    public FragmentContext Fragments { get; } = new();

    /// <summary>
    /// Gets a map exposing the variable definitions by name.
    /// </summary>
    public Dictionary<string, VariableDefinitionNode> Variables { get; } = [];

    /// <summary>
    /// The current visitation path of types.
    /// </summary>
    public List<IType> Types { get; } = [];

    /// <summary>
    /// The current visitation path of directive types.
    /// </summary>
    public List<IDirectiveDefinition> Directives { get; } = [];

    /// <summary>
    /// The current visitation path of output fields.
    /// </summary>
    public List<IOutputFieldDefinition> OutputFields { get; } = [];

    /// <summary>
    /// The current visitation path of selections.
    /// </summary>
    public List<FieldNode> Fields { get; } = [];

    /// <summary>
    /// The current visitation path of input fields.
    /// </summary>
    public List<IInputValueDefinition> InputFields { get; } = [];

    /// <summary>
    /// The feature collection that is used to execute the validation.
    /// </summary>
    public IFeatureCollection Features => _features;

    /// <summary>
    /// A dictionary to store arbitrary visitor data.
    /// </summary>
    public Dictionary<string, object?> ContextData { get; } = [];

    /// <summary>
    /// A list to track validation errors that occurred during the visitation.
    /// </summary>
    public IReadOnlyList<IError> Errors => _errors;

    /// <summary>
    /// Defines that a visitation has found an unexpected error
    /// that is no concern of the current validation rule.
    /// If no other error is found by any validation, this will
    /// lead to an unexpected validation error.
    /// </summary>
    public bool UnexpectedErrorsDetected { get; set; }

    /// <summary>
    /// Defines that a fatal error was detected and that the analyzer will be aborted.
    /// </summary>
    public bool FatalErrorDetected { get; set; }

    public void Initialize(
        ISchemaDefinition schema,
        OperationDocumentId documentId,
        DocumentNode document,
        int maxAllowedErrors,
        IFeatureCollection? features)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(document);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxAllowedErrors, 1);

        _schema = schema;
        DocumentId = documentId;
        Document = document;
        _maxAllowedErrors = maxAllowedErrors;

        _features.Initialize(features);

        foreach (var definitionNode in document.Definitions)
        {
            if (definitionNode.Kind is SyntaxKind.FragmentDefinition)
            {
                var fragmentDefinition = Unsafe.As<FragmentDefinitionNode>(definitionNode);
                Fragments[fragmentDefinition.Name.Value] = fragmentDefinition;
            }
        }
    }

    /// <summary>
    /// Reports an error.
    /// </summary>
    /// <param name="error">
    /// The validation error that shall be reported.
    /// </param>
    public void ReportError(IError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        _errors.Add(error);

        if (_errors.Count >= _maxAllowedErrors)
        {
            throw new MaxValidationErrorsException();
        }
    }

    /// <summary>
    /// Resets the context between document visitations.
    /// We keep the Schema, DocumentId, and the Fragment lookups.
    /// </summary>
    internal void Reset()
    {
        Path.Clear();
        SelectionSets.Clear();
        Fragments.Reset();
        Types.Clear();
        Directives.Clear();
        OutputFields.Clear();
        Fields.Clear();
        InputFields.Clear();

        // we just make sure that all features are reset but we do not want
        // to fully reset the feature collection.
        foreach (var feature in Features)
        {
            if (feature.Value is ValidatorFeature validatorFeature)
            {
                validatorFeature.Reset();
            }
        }
    }

    /// <summary>
    /// Clears the context fully after a full validation run is completed,
    /// and this context is returned to the pool.
    /// </summary>
    internal void Clear()
    {
        _schema = null;
        Document = null!;
        UnexpectedErrorsDetected = false;
        FatalErrorDetected = false;
        _features.Reset();

        Path.Clear();
        SelectionSets.Clear();
        Fragments.Clear();
        Variables.Clear();
        Types.Clear();
        Directives.Clear();
        OutputFields.Clear();
        Fields.Clear();
        InputFields.Clear();
        ContextData.Clear();
        _errors.Clear();
    }

    /// <summary>
    /// This context is used to track the fragments that are visited
    /// during the visitation of a document.
    /// </summary>
    public sealed class FragmentContext
    {
        private readonly HashSet<string> _visited = [];
        private readonly Dictionary<string, FragmentDefinitionNode> _fragments = new(StringComparer.Ordinal);

        public IEnumerable<string> Names => _fragments.Keys;

        public FragmentDefinitionNode this[string name]
        {
            get
            {
                return _fragments[name];
            }
            internal set
            {
                _fragments[name] = value;
            }
        }

        public bool TryGet(FragmentSpreadNode spread, [NotNullWhen(true)] out FragmentDefinitionNode? fragment)
            => _fragments.TryGetValue(spread.Name.Value, out fragment);

        public bool TryEnter(FragmentSpreadNode spread, [NotNullWhen(true)] out FragmentDefinitionNode? fragment)
        {
            if (_visited.Add(spread.Name.Value)
                && _fragments.TryGetValue(spread.Name.Value, out fragment))
            {
                return true;
            }

            fragment = null;
            return false;
        }

        public void Leave(FragmentSpreadNode spread)
            => _visited.Remove(spread.Name.Value);

        public void Leave(FragmentDefinitionNode fragment)
            => _visited.Remove(fragment.Name.Value);

        public bool Exists(FragmentSpreadNode spread)
            => _fragments.ContainsKey(spread.Name.Value);

        internal void Reset()
            => _visited.Clear();

        internal void Clear()
        {
            _visited.Clear();
            _fragments.Clear();
        }
    }
}
