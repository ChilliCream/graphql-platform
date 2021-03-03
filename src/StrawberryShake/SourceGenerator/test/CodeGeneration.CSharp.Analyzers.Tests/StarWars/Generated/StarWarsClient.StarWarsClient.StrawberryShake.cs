// StrawberryShake.CodeGeneration.CSharp.ClientGenerator

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers.StarWars
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class StarWarsClient
    {
        private readonly GetPeopleQuery _getPeopleQuery;

        public StarWarsClient(GetPeopleQuery getPeopleQuery)
        {
            _getPeopleQuery = getPeopleQuery
                 ?? throw new global::System.ArgumentNullException(nameof(getPeopleQuery));
        }

        public GetPeopleQuery GetPeopleQuery => _getPeopleQuery;
    }
}
