namespace HotChocolate.CodeGeneration;

public interface ICodeGenerator
{
    CodeGenerationResult Generate(CodeGeneratorContext context);
}