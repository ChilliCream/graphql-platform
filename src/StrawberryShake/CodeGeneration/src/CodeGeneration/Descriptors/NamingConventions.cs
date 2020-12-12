namespace StrawberryShake.CodeGeneration
{
    public static class NamingConventions
    {
        public static string EntityTypeNameFromTypeName(string typeName)
        {
            return typeName + "Entity";
        }

        public static string MapperNameFromTypeName(string typeName)
        {
            return typeName + "From" + EntityTypeNameFromTypeName(typeName) + "Mapper";
        }
    }
}
