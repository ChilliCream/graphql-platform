// StrawberryShake.CodeGeneration.CSharp.Generators.ClientGenerator

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers.StarWars
{
    /// <summary>
    /// Represents the StarWarsClient GraphQL client
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class StarWarsClient
    {
        private readonly global::StrawberryShake.CodeGeneration.CSharp.Analyzers.StarWars.GetPeopleQuery _getPeople;

        public StarWarsClient(global::StrawberryShake.CodeGeneration.CSharp.Analyzers.StarWars.GetPeopleQuery getPeople)
        {
            _getPeople = getPeople
                 ?? throw new global::System.ArgumentNullException(nameof(getPeople));
        }

        public static global::System.String ClientName => "StarWarsClient";

        public global::StrawberryShake.CodeGeneration.CSharp.Analyzers.StarWars.GetPeopleQuery GetPeople => _getPeople;
    }
}
