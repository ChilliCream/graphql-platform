namespace HotChocolate.Skimmed;

public interface IField : IHasName, IHasDirectives, IHasContextData, IHasDescription, ICanBeDeprecated
{
    IType Type { get; set; }
}
