namespace Mocha.Analyzers;

/// <summary>
/// Carries the evaluated <c>PublishAot</c> MSBuild property to source generators.
/// </summary>
/// <param name="IsAotPublish">
/// A value indicating whether the current compilation is being published for AOT.
/// </param>
public sealed record AotPublishInfo(bool IsAotPublish) : SyntaxInfo
{
    /// <inheritdoc />
    public override string OrderByKey => "AotPublish";
}
