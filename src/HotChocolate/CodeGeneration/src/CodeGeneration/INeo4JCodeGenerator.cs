namespace HotChocolate.CodeGeneration
{
    public interface INeo4JCodeGenerator
    {
        public CodeGenerationResult Generate(Neo4JCodeGeneratorContext context);
    }
}
