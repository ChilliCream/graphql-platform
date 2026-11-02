namespace Mocha.Sagas;

/// <summary>
/// Options for configuring how a saga sends point-to-point messages during transitions or lifecycle actions.
/// </summary>
public sealed class SagaSendOptions
{
    /// <summary>
    /// Gets or sets an optional factory that creates <see cref="SendOptions"/> from the consume context and saga state.
    /// </summary>
    public Func<IConsumeContext, object, SendOptions>? ConfigureOptions { get; set; }

    /// <summary>
    /// Gets the default send options with no custom configuration.
    /// </summary>
    public static SagaSendOptions Default { get; } = new();
}
