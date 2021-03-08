// StrawberryShake.CodeGeneration.CSharp.Generators.ResultFromEntityTypeMapperGenerator

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers.StarWars.State
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class GetPeople_People_Nodes_PersonFromPersonEntityMapper
        : global::StrawberryShake.IEntityMapper<global::StrawberryShake.CodeGeneration.CSharp.Analyzers.StarWars.State.PersonEntity, GetPeople_People_Nodes_Person>
    {
        public GetPeople_People_Nodes_Person Map(global::StrawberryShake.CodeGeneration.CSharp.Analyzers.StarWars.State.PersonEntity entity)
        {
            return new GetPeople_People_Nodes_Person(
                entity.Name,
                entity.Email,
                entity.IsOnline,
                entity.LastSeen);
        }
    }
}
