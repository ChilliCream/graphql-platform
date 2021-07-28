namespace HotChocolate.CodeGeneration
{
    public interface INeo4JCodeGenerator
    {
        CodeGenerationResult Generate(Neo4JCodeGeneratorContext context);
    }
}
