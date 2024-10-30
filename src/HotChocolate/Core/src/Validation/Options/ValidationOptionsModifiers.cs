namespace HotChocolate.Validation.Options;

public class ValidationOptionsModifiers
{
    public IList<Action<ValidationOptions>> Modifiers { get; } =
        new List<Action<ValidationOptions>>();

    public IList<Action<ValidationOptions, ValidationRulesOptions>> RulesModifiers { get; } =
        new List<Action<ValidationOptions, ValidationRulesOptions>>();
}
