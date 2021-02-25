#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers.StarWars
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class GetPeopleResultInfo
        : global::StrawberryShake.IOperationResultDataInfo
    {
        private readonly global::System.Collections.Generic.IReadOnlyCollection<global::StrawberryShake.EntityId> _entityIds;
        private readonly ulong _version;

        public GetPeopleResultInfo(
            global::StrawberryShake.CodeGeneration.CSharp.Analyzers.StarWars.State.PersonConnectionData? people,
            global::System.Collections.Generic.IReadOnlyCollection<global::StrawberryShake.EntityId> entityIds,
            ulong version)
        {
            People = people;
            _entityIds = entityIds
                 ?? throw new global::System.ArgumentNullException(nameof(entityIds));
            _version = version;
        }

        public global::StrawberryShake.CodeGeneration.CSharp.Analyzers.StarWars.State.PersonConnectionData? People { get; }

        public global::System.Collections.Generic.IReadOnlyCollection<global::StrawberryShake.EntityId> EntityIds => _entityIds;

        public ulong Version => _version;

        public global::StrawberryShake.IOperationResultDataInfo WithVersion(ulong version)
        {
            return new GetPeopleResultInfo(
                People,
                _entityIds,
                _version);
        }
    }
}
