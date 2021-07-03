# Adding complex filter capabilities

So far, our GraphQL server only exposes plain lists that would, at some point, grow so large that our server would time out. Moreover, we miss some filter capabilities for our session list so that the application using our backend can filter for tracks, titles, or search the abstract for topics.

## Add paging to your lists

Let us start by implementing the last Relay server specification we are still missing in our server by adding Relay compliant paging to our lists. In general, you should avoid plain lists wherever lists grow or are very large. Relay describes a cursor based paging where you can navigate between edges through their cursors. Cursor based paging is ideal whenever you implement infinite scrolling solutions. In contrast to offset-pagination, you cannot jump to a specific page, but you can jump to a particular cursor and navigate from there.

> Many database drivers or databases do not support `skip while`, so Hot Chocolate will under the hood use positions instead of proper IDs for cursors in theses cases. Meaning, you can always use cursor-based pagination, and Hot Chocolate will handle the rest underneath.

1. Head over to the `Tracks`directory and replace the `GetTracksAsync` resolver in the `TrackQueries.cs` with the following code.

   ```csharp
   [UseApplicationDbContext]
   [UsePaging]
   public IQueryable<Track> GetTracks(
       [ScopedService] ApplicationDbContext context) =>
       context.Tracks.OrderBy(t => t.Name);
   ```

   > The new resolver will instead of executing the database query return an `IQueryable`. The `IQueryable` is like a query builder. By applying the `UsePaging` middleware, we are rewriting the database query to only fetch the items that we need for our data-set.

   The resolver pipeline for our field now looks like the following:

   ![Paging Middleware Flow](images/22-pagination.png)

1. Start your GraphQL server.

   ```console
   dotnet run --project GraphQL
   ```

1. Open Banana Cake Pop and refresh the schema.

   ![Banana Cake Pop Root Fields](images/23-bcp-schema.png)

1. Head into the schema browser, and let us have a look at how our API structure has changed.

   ![Banana Cake Pop Tracks Field](images/24-bcp-schema.png)

1. Define a simple query to fetch the first track.

   ```graphql
   query GetFirstTrack {
     tracks(first: 1) {
       edges {
         node {
           id
           name
         }
         cursor
       }
       pageInfo {
         startCursor
         endCursor
         hasNextPage
         hasPreviousPage
       }
     }
   }
   ```

   ![Query speaker names](images/25-bcp-GetFirstTrack.png)

1. Take the cursor from this item and add a second argument after and feed in the cursor.

   ```graphql
   query GetNextItem {
     tracks(first: 1, after: "MA==") {
       edges {
         node {
           id
           name
         }
         cursor
       }
       pageInfo {
         startCursor
         endCursor
         hasNextPage
         hasPreviousPage
       }
     }
   }
   ```

   ![Query speaker names](images/26-bcp-GetNextTrack.png)

1. Head over to the `SpeakerQueries.cs` which are located in the `Speakers` directory and replace the `GetSpeakersAsync` resolver with the following code:

   ```csharp
   [UseApplicationDbContext]
   [UsePaging]
   public IQueryable<Speaker> GetSpeakers(
       [ScopedService] ApplicationDbContext context) =>
       context.Speakers.OrderBy(t => t.Name);
   ```

1. Next, go to the `SessionQueries.cs` in the `Sessions` directory and replace the `GetSessionsAsync` with the following code:

   ```csharp
   [UseApplicationDbContext]
   [UsePaging]
   public IQueryable<Session> GetSessions(
       [ScopedService] ApplicationDbContext context) =>
       context.Sessions;
   ```

   We have now replaced all the root level list fields and are now using our pagination middleware. There are still more lists left where we should apply pagination if we wanted to really have a refined schema. Let us change the API a bit more to incorporate this.

1. First, go back to the `SessionQueries.cs` in the `Sessions` directory and replace the `[UsePaging]` with `[UsePaging(typeof(NonNullType<SessionType>))]`.

   ```csharp
   [UseApplicationDbContext]
   [UsePaging(typeof(NonNullType<SessionType>))]
   public IQueryable<Session> GetSessions(
       [ScopedService] ApplicationDbContext context) =>
       context.Sessions;
   ```

   > It is important that a connection type works with a fixed item type if we mix attribute and fluent syntax.

1. Next, open the `TrackType.cs` in the `Types` directory and add `.UsePaging<NonNullType<SessionType>>()` to the `Sessions` field descriptor.

   ```csharp
   descriptor
       .Field(t => t.Sessions)
       .ResolveWith<TrackResolvers>(t => t.GetSessionsAsync(default!, default!, default!, default))
       .UseDbContext<ApplicationDbContext>()
       .UsePaging<NonNullType<SessionType>>()
       .Name("sessions");
   ```

1. Now go back to Banana Cake Pop and refresh the schema.

   ![Inspect Track Session](images/27-bcp-schema.png)

1. Fetch a specific track and get the first session of this track:

   ```graphql
   query GetTrackWithSessions {
     trackById(id: "VHJhY2sKaTI=") {
       id
       sessions(first: 1) {
         nodes {
           title
         }
       }
     }
   }
   ```

   ![Query speaker names](images/28-bcp-GetTrackWithSessions.png)

   > There is one caveat in our implementation with the `TrackType`. Since, we are using a DataLoader within our resolver and first fetch the list of IDs we essentially will always fetch everything and chop in memory. In an actual project this can be split into two actions by moving the `DataLoader` part into a middleware and first page on the id queryable. Also one could implement a special `IPagingHandler` that uses the DataLoader and applies paging logic.

## Add filter capabilities to the top-level field `sessions`

Exposing rich filters to a public API can lead to unpredictable performance implications, but using filters wisely on select fields can make your API much better to use. In our conference API it would make almost no sense to expose filters on top of the `tracks` field since the `Track` type really only has one field `name` and filtering on that really seems overkill. The `sessions` field on the other hand could be improved with filter capabilities. The user of our conference app could with filters search for a session in a specific time-window or for sessions of a specific speaker he/she likes.

Filters like paging is a middleware that can be applied on `IQueryable`, like mentioned in the middleware session order is important with middleware. This means our paging middleware has to execute last.

![Filter Middleware Flow](images/20-middleware-flow.png)

1. Add a reference to the NuGet package package `HotChocolate.Data` version `11.0.0`.

   1. `dotnet add GraphQL package HotChocolate.Data --version 11.0.0`

1. Add filter and sorting conventions to the schema configuration.

   ```csharp
   services
      .AddGraphQLServer()
      .AddQueryType(d => d.Name("Query"))
         .AddTypeExtension<SpeakerQueries>()
         .AddTypeExtension<SessionQueries>()
         .AddTypeExtension<TrackQueries>()
      .AddMutationType(d => d.Name("Mutation"))
         .AddTypeExtension<SessionMutations>()
         .AddTypeExtension<SpeakerMutations>()
         .AddTypeExtension<TrackMutations>()
      .AddType<AttendeeType>()
      .AddType<SessionType>()
      .AddType<SpeakerType>()
      .AddType<TrackType>()
      .EnableRelaySupport()
      .AddFiltering()
      .AddSorting()
      .AddDataLoader<SpeakerByIdDataLoader>()
      .AddDataLoader<SessionByIdDataLoader>();
   ```

1. Head over to the `SessionQueries.cs` which is located in the `Sessions` directory.

1. Replace the `GetSessions` resolver with the following code:

   ```csharp
   [UseApplicationDbContext]
   [UsePaging(typeof(NonNullType<SessionType>))]
   [UseFiltering]
   [UseSorting]
   public IQueryable<Session> GetSessions(
       [ScopedService] ApplicationDbContext context) =>
       context.Sessions;
   ```

   > By default the filter middleware would infer a filter type that exposes all the fields of the entity. In our case it would be better to remove filtering for ids and internal fields and focus on fields that the user really can use.

1. Create a new `SessionFilterInputType.cs` in the `Sessions` directory with the following code:

   ```csharp
   using ConferencePlanner.GraphQL.Data;
   using HotChocolate.Data.Filters;

   namespace ConferencePlanner.GraphQL.Types
   {
      public class SessionFilterInputType : FilterInputType<Session>
      {
         protected override void Configure(IFilterInputTypeDescriptor<Session> descriptor)
         {
               descriptor.Ignore(t => t.Id);
               descriptor.Ignore(t => t.TrackId);
         }
      }
   }
   ```

   > We essentially have remove the ID fields and leave the rest in.

1. Go back to the `SessionQueries.cs` which is located in the `Sessions` directory and replace the `[UseFiltering]` attribute on top of the `GetSessions` resolver with the following `[UseFiltering(typeof(SessionFilterInputType))]`.

   ```csharp
   [UseApplicationDbContext]
   [UsePaging(typeof(NonNullType<SessionType>))]
   [UseFiltering(typeof(SessionFilterInputType))]
   [UseSorting]
   public IQueryable<Session> GetSessions(
       [ScopedService] ApplicationDbContext context) =>
       context.Sessions;
   ```

1. Start your GraphQL server.

   ```console
   dotnet run --project GraphQL
   ```

1. Open Banana Cake Pop and refresh the schema and head over to the schema browser.

   ![Session Filter Type](images/29-bcp-filter-type.png)

   > We now have an argument `where` on our field that exposes a rich filter type to us.

1. Write the following query to look for all the sessions that contain `2` in their title.

   ```graphql
   query GetSessionsContaining2InTitle {
     sessions(where: { title: { contains: "2" } }) {
       nodes {
         title
       }
     }
   }
   ```

   ![Apply Filter on Sessions](images/30-bcp-get-sessions.png)

## Summary

With cursor base pagination, we have introduced a strong pagination concept and also put the last piece in to be fully Relay compliant. We have learned that we can page within a paged result; in fact, you can create large paging hierarchies.

Further, we have looked at filtering where we can apply a simple middleware that infers from our data model a powerful filter structure. Filters are rewritten into native database queries on top of `IQueryable` but can also be applied to in-memory lists. Use filters where they make sense and control them by providing filter types that limit what a user can do to keep performance predictable.

[**<< Session #5 - Understanding middleware**](5-understanding-middleware.md) | [**Session #7 - Adding real-time functionality with subscriptions >>**](7-subscriptions.md) 
