using System;
using System.Reflection;
using HotChocolate.Execution.Configuration;

namespace HotChocolate.Stitching.SchemaDefinitions;

public interface IPublishSchemaDefinitionDescriptor
{
    /// <summary>
    /// Sets the configuration name.
    /// </summary>
    /// <param name="name">
    /// The configuration name.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IPublishSchemaDefinitionDescriptor"/>
    /// </returns>
    IPublishSchemaDefinitionDescriptor SetName(string  name);

    IPublishSchemaDefinitionDescriptor AddTypeExtensionsFromFile(
        string fileName);

    IPublishSchemaDefinitionDescriptor AddTypeExtensionsFromResource(
        Assembly assembly,
        string key);

    IPublishSchemaDefinitionDescriptor AddTypeExtensionsFromString(
        string schemaSdl);

    IPublishSchemaDefinitionDescriptor SetSchemaDefinitionPublisher(
        Func<IServiceProvider, ISchemaDefinitionPublisher> publisherFactory);

    IPublishSchemaDefinitionDescriptor IgnoreRootTypes();

    IPublishSchemaDefinitionDescriptor IgnoreType(
        string typeName);

    IPublishSchemaDefinitionDescriptor RenameType(
        string typeName,
        string newTypeName);

    IPublishSchemaDefinitionDescriptor RenameField(
        string typeName,
        string fieldName,
        string newFieldName);
}
