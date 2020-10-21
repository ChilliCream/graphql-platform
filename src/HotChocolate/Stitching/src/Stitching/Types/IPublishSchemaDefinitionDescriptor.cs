using System.Reflection;

namespace HotChocolate.Stitching.Types
{
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
        IPublishSchemaDefinitionDescriptor SetName(NameString  name);

        IPublishSchemaDefinitionDescriptor AddTypeExtensionsFromFile(
            string fileName);

        IPublishSchemaDefinitionDescriptor AddTypeExtensionsFromResource(
            Assembly assembly,
            string key);

        IPublishSchemaDefinitionDescriptor AddTypeExtensionsFromString(
            string schemaSdl);

        IPublishSchemaDefinitionDescriptor IgnoreRootTypes();

        IPublishSchemaDefinitionDescriptor IgnoreType(
            NameString typeName);

        IPublishSchemaDefinitionDescriptor RenameType(
            NameString typeName,
            NameString newTypeName);

        IPublishSchemaDefinitionDescriptor RenameField(
            NameString typeName,
            NameString fieldName,
            NameString newFieldName);
    }
}
