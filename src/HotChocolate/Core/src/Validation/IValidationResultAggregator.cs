using HotChocolate.Language;

namespace HotChocolate.Validation;

/// <summary>
/// The result aggregator allows for async result aggregation of previously run validation rules.
/// </summary>
public interface IValidationResultAggregator
{
    /// <summary>
    /// Aggregates the result of validation rules and may produce errors from them.
    /// </summary>
    /// <param name="context">
    /// The validation context.
    /// </param>
    /// <param name="document">
    /// The GraphQL document that shall be inspected.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    ValueTask AggregateAsync(
        IDocumentValidatorContext context,
        DocumentNode document,
        CancellationToken cancellationToken = default);
}
