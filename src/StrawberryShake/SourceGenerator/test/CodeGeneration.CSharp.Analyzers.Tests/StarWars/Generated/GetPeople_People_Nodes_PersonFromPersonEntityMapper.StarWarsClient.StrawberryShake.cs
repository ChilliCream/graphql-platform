#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers.StarWars
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class GetPeople_People_Nodes_PersonFromPersonEntityMapper
        : global::StrawberryShake.IEntityMapper<PersonEntity, GetPeople_People_Nodes_Person>
    {
        public GetPeople_People_Nodes_Person Map(PersonEntity entity)
        {
            return new GetPeople_People_Nodes_Person(
                entity.Name,
                entity.Email,
                entity.IsOnline,
                entity.LastSeen);
        }
    }
}
