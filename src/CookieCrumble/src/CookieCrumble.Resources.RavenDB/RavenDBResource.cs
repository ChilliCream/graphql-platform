using System.Runtime.InteropServices;
using Raven.Client.Documents;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;
using Testcontainers.RavenDb;

namespace CookieCrumble.Resources;

public class RavenDBResource : ContainerResource<RavenDbContainer>
{
    public string ConnectionString => Container.GetConnectionString();

    public IDocumentStore CreateDatabase(string name)
    {
        using (var store = GetDocumentStore())
        {
            store.Maintenance.Server.Send(new CreateDatabaseOperation(new DatabaseRecord(name)));
        }

        return GetDocumentStore(name);
    }

    public IDocumentStore GetDocumentStore(string? databaseName = null)
    {
        var store = new DocumentStore { Urls = [ConnectionString], Database = databaseName };
        store.Initialize();

        return store;
    }

    protected override RavenDbContainer Build()
        => Configure(
            new RavenDbBuilder(
                RuntimeInformation.ProcessArchitecture is Architecture.Arm64
                    ? "ravendb/ravendb:6.2-ubuntu-arm64v8-latest"
                    : "ravendb/ravendb:6.2-ubuntu-latest"))
            .Build();

    protected virtual RavenDbBuilder Configure(RavenDbBuilder builder) => builder;
}
