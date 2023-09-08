---
title: "Schema & Client Registry"
---

The schema and client registries are essential tools for managing your GraphQL APIs. They provide a centralized location for storing, managing, and distributing your GraphQL schema and client definitions. 

With the schema registry, you can upload and store the schema of your API, making it accessible to your development team and other services. In parallel, the client registry allows you to manage the clients of your API, each of which can have multiple versions. A version here refers to a collection of query documents.

The registries enable you to validate your schemas and clients against previous versions, ensuring that changes to your service do not break existing functionality. They also maintain a version history, allowing you to track changes over time and revert to previous versions if necessary.

By working harmoniously, the schema and client registries help maintain the integrity of your API and the services that rely on it, ensuring that they can evolve together without breaking.

# Understanding Schemas

In the context of GraphQL APIs, a schema is the blueprint that defines the shape of your data and specifies the capabilities of the API. It outlines the types of queries and mutations that can be executed against your API. 
A schema is more than just a technical specification; it is a contract between your API and your clients. By understanding and managing schema changes, you can ensure that this contract remains valid and that your API and clients can evolve together without breaking.

Each stage of your API can have one active schema. This active schema is the one against which all requests are validated. 

## Schema Changes

Changes to the schema can be categorized into three levels of severity based on their potential impact on the clients: safe, dangerous, and breaking.

1. **Safe**: These changes don't affect the existing functionality. Examples include changes to descriptions or adding a new optional field to a type. Safe changes are generally backward compatible and don't require modifications to existing clients.

2. **Dangerous**: These changes could potentially break existing functionality, depending on how your consumers interact with your API. An example of a dangerous change is adding a new member to an enum. If the client is not prepared to handle the new member, it might result in unexpected behavior.

3. **Breaking**: These changes will break existing functionality if the affected parts of the schema are being used by clients. Examples include changing the type of a field, adding a required field to an input type, removing a field, or adding a new required argument to a field or directive.

Breaking changes need to be managed with care to avoid disruptions to the service. It's important to ensure that all clients can handle these changes before they are introduced. This can be accomplished by versioning your clients and managing the lifecycle of client versions, as described in the section [Understanding Clients](#understanding-clients)].

## Extracting the Schema 

Extracting your GraphQL API's schema can be beneficial for various purposes, such as documentation, testing, and version control. Here are some methods to extract the schema:

### Using Schema Export Command

One of the simplest ways to extract the schema is by using the `schema export` command. This command exports your current schema into a specified output file.

```shell
dotnet run -- schema export --output schema.graphql
```

For more details about this command and how to setup the command line extension, please refer to the [Command Line Extension documentation](/docs/hotchocolate/v13/server/command-line).

### Utilizing Snapshot Testing

If you have already established snapshot testing in your workflow, you can use it to extract the schema. Snapshot tests compare the current schema against a previously saved one. If the schemas differ, the test fails, ensuring unintentional schema changes are detected.

Additionally, keeping a snapshot test in the repository aids in visualizing schema changes in pull requests.

Here is a sample snapshot test using [Snapshooter](https://github.com/SwissLife-OSS/snapshooter):

```csharp
[Fact]
public async Task Schema_Should_Not_Change()
{
  // Arrange
  var executor = await new ServiceCollection()
    .AddGraphQL()
    .AddYourSchema()
    .BuildRequestExecutorAsync();

  // Act
  var schema = executor.Schema.Print();

  // Assert
  schema.MatchSnapshot();
}
```

# Understanding Clients 

A client, in the context of a GraphQL API, is an entity that interacts with the API by defining and executing GraphQL operations. These operations are stored on the API as persisted queries.

## What is a Persisted Query?

A persisted query is a GraphQL operation that has been sent to the server, stored, and assigned an unique identifier (hash). Instead of sending the full text of a GraphQL operation to the server for execution, clients can send the hash of the operation, reducing the amount of data transmitted over the network. This practice is particularly beneficial for mobile clients operating in environments with limited network capacity.

Persisted queries also add an extra layer of security as the server can be configured to only execute operations that have been previously stored, which prevents malicious queries. This is the cheapest and most effective way to secure your GraphQL API from potential attacks.

## The Role of the Client Registry

The client registry plays a crucial role in managing these persisted queries. It is used to validate the queries against the schema, ensuring that all the operations defined by a client are compatible with the current schema. This validation step is critical to prevent the execution of invalid queries that might result in runtime errors.

Additionally, the client registry is responsible for distributing the queries to the GraphQL server. It maintains a mapping of hashes to query keys, informing the server which hash corresponds to which query. This allows the server to efficiently look up and execute the appropriate query when it receives a request from a client.

## Client Versions

A client can have multiple versions, with each version containing a different set of persisted queries. This versioning system allows for incremental updates and changes to the client's operations without disrupting the existing functionality. As new versions are released, they can be validated and registered with the client registry, ensuring that they are compatible with the current schema and can be executed by the server.

By managing client versions and persisted queries, the client registry helps maintain the integrity and smooth operation of your GraphQL API. It ensures that your clients and API can evolve together without breaking, contributing to a more robust and reliable system.

The number of active client versions can vary depending on the nature of the client. For instance, a website usually has one active client version per stage. However, during deployment, you might temporarily have two active versions as the new version is phased in and the old version is phased out.

On the other hand, for mobile clients, you often have multiple versions active simultaneously. This is because users may be using different versions of the app, and not all users update their apps at the same time.

Once a client version is no longer in use, it reaches its end of life. At this point, you can unpublish the client version from the client registry. This will remove its persisted queries from distribution, and they will no longer be validated against the schema. 

## The Operations File

In the context of GraphQL, the operations file is a structured file that holds a collection of persisted queries for a client. This file serves as a reference for the client to manage and execute specific operations against a GraphQL API. 

### Understanding the Format and Structure

The operations file typically adopts the JSON format as used by Relay. It comprises key-value pairs, with each pair representing a unique persisted query. The key corresponds to a hash identifier for the query, and the value is the GraphQL query string. Below is an illustrative example of an operations file (`operations.json`):

```json
{
   "913abc361487c481cf6015841c0eca22": "{ me { username } }",
   "0e7cf2125e8eb711b470cc72c73ca77e": "{ me { id } }"
   ...
}
```

### Compatibility with GraphQL Clients

Several GraphQL clients have built-in support for this Relay-style operations file format. This compatibility allows for a standardized way of handling persisted queries across different clients. For more details on how various clients implement and work with persisted queries, consider referring to their respective documentation:

- [StrawberryShake](https://chillicream.com/docs/strawberryshake/v13/performance/persisted-queries)
- [URQL](https://formidable.com/open-source/urql/docs/advanced/persisted-queries/)
- [Relay](https://relay.dev/docs/guides/persisted-queries/)

# Working with Stages
A stage represents an environment of your service, such as development, staging, or production. Each stage can have an active schema and multiple active client versions. 

Stages are integral to the lifecycle management of your GraphQL APIs. They enable you to manage different environments of your service, such as development, staging, or production. Each stage can have an active schema and multiple active client versions. The active schema and client versions for a stage represent the current state of your API for that environment.

Stages in your development workflow can be arranged sequentially to represent progression of changes. For instance, in a simple flow like Development (Dev) - Quality Assurance (QA) - Production (Prod), each stage comes "after" the preceding one. This signifies that changes propagate from "Dev" to "QA", and finally to "Prod"

## Managing Stages

To manage stages, you'll use the `barista stage edit` command. After executing this command, you first select the API you want to modify. Then, you can add, edit, or delete stages using the provided interface. 

The interface displays a table showing the current stages, their names, and their order (defined in the 'After' column). Utilize the provided keys to add a new stage, save changes, edit a stage, or delete a stage.

When you add or edit a stage, you'll need to provide a name for the stage and specify where it should be placed in the order of stages. When you delete a stage, be aware that this will also remove the active schema and client versions for that stage.

# Setting Up a Schema Registry

To set up a schema registry, first, visit `eat.bananacakepop.com` and sign up for an account. Next, you'll need to download and install Barista, the .NET tool used to manage your schema registry. You can find more information about Barista in the [Barista Documentation](/docs/barista/v1). 

After installing Barista, create a new API either through the Bananacakepop UI or the CLI. In the UI, simply right-click the document explorer and select "New API." If you prefer using the CLI, ensure you're logged in with the command `barista login`, then create a new API with the command `barista api create`. With these steps complete, you are ready to start using the schema registry.

To get the id of your API, use the command `barista api list`. This command will list all of your APIs, their names, and their ids. You will need the id of your API to perform most operations on the schema registry. 

# Integrating with Continuous Integration

Integrating the schema registry and the client registry into your Continuous Integration/Continuous Deployment (CI/CD) pipeline maximizes their benefits. It ensures that the schemas and clients in your API are always up-to-date and tested against potential breaking changes.

The schema and client registries work hand-in-hand to ensure the smooth functioning of your API. As you make changes to your schema, the schema registry helps manage these changes, preventing inadvertent breaking changes and preserving a history of your schemas. As you validate, upload, and publish new schemas, the client registry ensures that your clients remain compatible with these changes.

As you release new versions of your clients, the client registry helps manage these versions and the query documents associated with them. By working together, the schema and client registries help maintain the integrity of your API and the services that rely on it, ensuring that they can evolve together without breaking.

## Understanding the Flow

The general flow for the schema registry involves three main steps: validating the schema, uploading it to the registry, and publishing it.

1. **Validate the Schema / Client**: The first step takes place during your Pull Request (PR) build. Here, you validate the schema or client against the API using the `barista schema validate` or `barista client validate` commands. This ensures that the schema or client is compatible with the API and will not break existing functionality. 

2. **Upload the Schema / Client**: The second step takes place during your release build. Here, you upload the schema or client to the registry using the `barista schema upload` or `barista client upload` commands. This command requires the `--tag` and `--api-id` options. The `--tag` option specifies the tag for the schema or client, and the `--api-id` option specifies the ID of the API to which you are uploading. This command create a new version of the schema or client with the specified tag.
The tag is a string that can be used to identify the schema or client. It can be any string, but it is recommended to use a version number, such as `v1` or `v2`; or a commit hash, such as `a1b2c3d4e5f6g7h8i9j0k1l2m3n`.  The tag is used to identify the schema or client when publishing it.

3. **Publish the Schema / Client **: The third step takes place just before the release. Here, you publish the schema or client using the `barista schema publish` or `barista client publish` commands. This command requires the `--tag` and `--api-id` options. The `--tag` option specifies the tag for the schema or client, and the `--api-id` option specifies the ID of the API to which you are uploading. This command publishes the schema or client with the specified tag, making it the active version for the specified API.
