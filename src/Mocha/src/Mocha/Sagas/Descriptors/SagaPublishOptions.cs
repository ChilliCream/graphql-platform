using Mocha;

namespace Mocha.Sagas;

/// <summary>
/// Options for configuring how a saga publishes messages during transitions or lifecycle actions.
/// </summary>
public sealed class SagaPublishOptions
{
    /// <summary>
    /// Gets or sets an optional factory that creates <see cref="PublishOptions"/> from the consume context and saga state.
    /// </summary>
    public Func<IConsumeContext, object, PublishOptions>? ConfigureOptions { get; set; }

    /// <summary>
    /// Gets the default publish options with no custom configuration.
    /// </summary>
    public static SagaPublishOptions Default { get; } = new();
}
