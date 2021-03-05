// StrawberryShake.CodeGeneration.CSharp.Generators.ResultTypeGenerator

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers.StarWars
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class GetPeople_People_Nodes_Person
        : IGetPeople_People_Nodes_Person
    {
        public GetPeople_People_Nodes_Person(
            global::System.String name,
            global::System.String email,
            global::System.Boolean isOnline,
            global::System.DateTimeOffset lastSeen)
        {
            Name = name;
            Email = email;
            IsOnline = isOnline;
            LastSeen = lastSeen;
        }

        public global::System.String Name { get; }

        public global::System.String Email { get; }

        public global::System.Boolean IsOnline { get; }

        public global::System.DateTimeOffset LastSeen { get; }
    }
}
