namespace HotChocolate.Skimmed;

public interface IField : INameProvider, IDirectivesProvider, IHasContextData, IDescriptionProvider, IDeprecationProvider
{
    ITypeDefinition Type { get; set; }
}
