// StrawberryShake.CodeGeneration.CSharp.Generators.ResultTypeGenerator

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers.StarWars
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class GetPeopleResult
        : IGetPeopleResult
    {
        public GetPeopleResult(IGetPeople_People? people)
        {
            People = people;
        }

        /// <summary>
        /// Gets access to all the people known to this service.
        /// </summary>
        public IGetPeople_People? People { get; } = default!;
    }
}
