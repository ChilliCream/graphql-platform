// StrawberryShake.CodeGeneration.CSharp.Generators.ResultDataFactoryGenerator

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers.StarWars.State
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class GetPeopleResultFactory
        : global::StrawberryShake.IOperationResultDataFactory<GetPeopleResult>
    {
        private readonly global::StrawberryShake.IEntityStore _entityStore;
        private readonly global::StrawberryShake.IEntityMapper<global::StrawberryShake.CodeGeneration.CSharp.Analyzers.StarWars.State.PersonEntity, GetPeople_People_Nodes_Person> _getPeople_People_Nodes_PersonFromPersonEntityMapper;

        public GetPeopleResultFactory(
            global::StrawberryShake.IEntityStore entityStore,
            global::StrawberryShake.IEntityMapper<global::StrawberryShake.CodeGeneration.CSharp.Analyzers.StarWars.State.PersonEntity, GetPeople_People_Nodes_Person> getPeople_People_Nodes_PersonFromPersonEntityMapper)
        {
            _entityStore = entityStore
                 ?? throw new global::System.ArgumentNullException(nameof(entityStore));
            _getPeople_People_Nodes_PersonFromPersonEntityMapper = getPeople_People_Nodes_PersonFromPersonEntityMapper
                 ?? throw new global::System.ArgumentNullException(nameof(getPeople_People_Nodes_PersonFromPersonEntityMapper));
        }

        public GetPeopleResult Create(
            global::StrawberryShake.IOperationResultDataInfo dataInfo,
            global::StrawberryShake.IEntityStoreSnapshot? snapshot = null)
        {
            if (snapshot is null)
            {
                snapshot = _entityStore.CurrentSnapshot;
            }

            if (dataInfo is GetPeopleResultInfo info)
            {
                return new GetPeopleResult(MapIGetPeople_People(
                    info.People,
                    snapshot));
            }

            throw new global::System.ArgumentException("GetPeopleResultInfo expected.");
        }

        private global::StrawberryShake.CodeGeneration.CSharp.Analyzers.StarWars.IGetPeople_People? MapIGetPeople_People(
            global::StrawberryShake.CodeGeneration.CSharp.Analyzers.StarWars.State.PersonConnectionData? data,
            global::StrawberryShake.IEntityStoreSnapshot snapshot)
        {
            if (data is null)
            {
                return null;
            }

            IGetPeople_People returnValue = default!;

            if (data?.__typename.Equals(
                    "PersonConnection",
                    global::System.StringComparison.Ordinal) ?? false)
            {
                returnValue = new GetPeople_People_PersonConnection(MapIGetPeople_People_NodesArray(
                    data.Nodes,
                    snapshot));
            }
            else
            {
                throw new global::System.NotSupportedException();
            }
            return returnValue;
        }

        private global::System.Collections.Generic.IReadOnlyList<global::StrawberryShake.CodeGeneration.CSharp.Analyzers.StarWars.IGetPeople_People_Nodes?>? MapIGetPeople_People_NodesArray(
            global::System.Collections.Generic.IReadOnlyList<global::StrawberryShake.EntityId?>? list,
            global::StrawberryShake.IEntityStoreSnapshot snapshot)
        {
            if (list is null)
            {
                return null;
            }

            var persons = new global::System.Collections.Generic.List<global::StrawberryShake.CodeGeneration.CSharp.Analyzers.StarWars.IGetPeople_People_Nodes?>();

            foreach (global::StrawberryShake.EntityId? child in list)
            {
                persons.Add(MapIGetPeople_People_Nodes(
                    child,
                    snapshot));
            }

            return persons;
        }

        private global::StrawberryShake.CodeGeneration.CSharp.Analyzers.StarWars.IGetPeople_People_Nodes? MapIGetPeople_People_Nodes(
            global::StrawberryShake.EntityId? entityId,
            global::StrawberryShake.IEntityStoreSnapshot snapshot)
        {
            if (entityId is null)
            {
                return null;
            }


            if (entityId.Value.Name.Equals(
                    "Person",
                    global::System.StringComparison.Ordinal))
            {
                return _getPeople_People_Nodes_PersonFromPersonEntityMapper.Map(
                    snapshot.GetEntity<global::StrawberryShake.CodeGeneration.CSharp.Analyzers.StarWars.State.PersonEntity>(entityId.Value)
                        ?? throw new global::StrawberryShake.GraphQLClientException());
            }
            throw new global::System.NotSupportedException();
        }
    }
}
