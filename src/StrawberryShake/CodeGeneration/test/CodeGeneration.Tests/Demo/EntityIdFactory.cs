[global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
public static partial class EntityIdFactory
{
    public static global::StrawberryShake.EntityId CreateEntityId(global::System.Text.Json.JsonElement obj)
    {
        global::System.String typeName = obj.GetProperty("__typename").GetString()!;
        
        return typeName switch
        {
            "Droid" => CreateDroidEntityId(obj, typeName),
            "Human" => CreateHumanEntityId(obj, typeName),
            _ => throw new global::System.NotSupportedException()
        };
    }

    private static global::StrawberryShake.EntityId CreateDroidEntityId(
        global::System.Text.Json.JsonElement obj,
        global::System.String type)
    {
        return new global::StrawberryShake.EntityId(
            type,
            obj.GetProperty("id").GetString()!);
    }

    private static global::StrawberryShake.EntityId CreateHumanEntityId(
        global::System.Text.Json.JsonElement obj,
        global::System.String type)
    {
        return new global::StrawberryShake.EntityId(
            type,
            obj.GetProperty("id").GetString()!);
    }
}
