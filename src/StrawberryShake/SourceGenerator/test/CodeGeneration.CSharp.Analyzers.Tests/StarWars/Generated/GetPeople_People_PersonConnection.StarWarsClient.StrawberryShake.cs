// StrawberryShake.CodeGeneration.CSharp.Generators.ResultTypeGenerator

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers.StarWars
{
    /// <summary>
    /// A connection to a list of items.
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class GetPeople_People_PersonConnection
        : IGetPeople_People_PersonConnection
    {
        public GetPeople_People_PersonConnection(global::System.Collections.Generic.IReadOnlyList<IGetPeople_People_Nodes?>? nodes)
        {
            Nodes = nodes;
        }

        /// <summary>
        /// A flattened list of the nodes.
        /// </summary>
        public global::System.Collections.Generic.IReadOnlyList<IGetPeople_People_Nodes?>? Nodes { get; } = default!;
    }
}
