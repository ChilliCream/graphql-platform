using StrawberryShake.CodeGeneration.Analyzers.Models;

namespace StrawberryShake.CodeGeneration.Analyzers;

internal abstract class SelectionSetAnalyzer
{
    public abstract OutputTypeModel Analyze(
        IDocumentAnalyzerContext context,
        FieldSelection fieldSelection,
        SelectionSetVariants selectionSetVariants);
}
