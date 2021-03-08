// StrawberryShake.CodeGeneration.CSharp.Generators.ResultInfoGenerator

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers.StarWars.State
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.1.0.0")]
    public partial class GetPeopleResultInfo
        : global::StrawberryShake.IOperationResultDataInfo
    {
        private readonly global::System.Collections.Generic.IReadOnlyCollection<global::StrawberryShake.EntityId> _entityIds;
        private readonly global::System.UInt64 _version;

        public GetPeopleResultInfo(
            global::StrawberryShake.CodeGeneration.CSharp.Analyzers.StarWars.State.PersonConnectionData? people,
            global::System.Collections.Generic.IReadOnlyCollection<global::StrawberryShake.EntityId> entityIds,
            global::System.UInt64 version)
        {
            People = people;
            _entityIds = entityIds
                 ?? throw new global::System.ArgumentNullException(nameof(entityIds));
            _version = version;
        }

        /// <summary>
        /// Gets access to all the people known to this service.
        /// </summary>
        public global::StrawberryShake.CodeGeneration.CSharp.Analyzers.StarWars.State.PersonConnectionData? People { get; }

        public global::System.Collections.Generic.IReadOnlyCollection<global::StrawberryShake.EntityId> EntityIds => _entityIds;

        public global::System.UInt64 Version => _version;

        public global::StrawberryShake.IOperationResultDataInfo WithVersion(global::System.UInt64 version)
        {
            return new GetPeopleResultInfo(
                People,
                _entityIds,
                version);
        }
    }
}
