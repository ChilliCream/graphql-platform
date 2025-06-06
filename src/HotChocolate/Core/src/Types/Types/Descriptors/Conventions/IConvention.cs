namespace HotChocolate.Types.Descriptors;

/// <summary>
/// This is a marker interface to collect registered type conventions from
/// the dependency injection container.
/// </summary>
public interface IConvention : IHasScope;
