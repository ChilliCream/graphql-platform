#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers.StarWars
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class GetPeople
        : IGetPeople
    {
        public GetPeople(IGetPeople_People? people)
        {
            People = people;
        }

        public IGetPeople_People? People { get; } = default!;
    }
}
