namespace HotChocolate.CostAnalysis;

internal static class GateThrowHelper
{
    public static InvalidOperationException RepositoryRootNotFound()
        => new("Could not locate the repository root.");

    public static InvalidOperationException ConfigurationCouldNotBeRead(string path)
        => new($"The gate configuration could not be read from '{path}'.");

    public static InvalidOperationException ProvenanceCouldNotBeRead(string path)
        => new($"The head-to-head provenance could not be read from '{path}'.");

    public static InvalidOperationException UnexpectedRepositoryChanges(string path)
        => new(
            $"The current-run provenance in '{path}' has tracked or untracked changes outside the generated artifacts.");

    public static InvalidOperationException InvalidCommittedProvenance(string path)
        => new(
            $"The committed-artifact provenance in '{path}' does not cover only generated artifact commits.");

    public static InvalidOperationException InvalidConfiguration(string message)
        => new($"The gate configuration is invalid: {message}");

    public static InvalidOperationException EmptyCsv(string path)
        => new($"The benchmark result '{path}' is empty.");

    public static InvalidOperationException InvalidCsv(string path, int lineNumber)
        => new($"The benchmark result '{path}' has an invalid row at line {lineNumber}.");

    public static InvalidOperationException MissingColumn(string path, string column)
        => new($"The benchmark result '{path}' does not contain the '{column}' column.");

    public static InvalidOperationException RowNotFound(string path, string description)
        => new($"The benchmark result '{path}' does not contain {description}.");

    public static InvalidOperationException UnexpectedRowCount(
        string path,
        string description,
        int actual)
        => new($"The benchmark result '{path}' contains {actual} {description} rows, expected 10.");

    public static InvalidOperationException UnexpectedEndpoints(string path)
        => new($"The benchmark result '{path}' does not contain the three frozen endpoints.");

    public static InvalidOperationException InvalidValue(
        string path,
        string column,
        string value)
        => new($"The benchmark result '{path}' has invalid {column} value '{value}'.");

    public static InvalidOperationException UnexpectedBudgetResult(
        int variableCount,
        int caseBudget,
        bool expected,
        bool actual)
        => new(
            $"The {variableCount}-variable adversarial plan with budget {caseBudget} "
            + $"reported HitCaseBudget={actual}, expected {expected}.");

    public static InvalidOperationException GitRevisionCouldNotBeRead(string message)
        => new($"The current Git revision could not be read: {message}");

    public static InvalidOperationException InvalidGitPathOutput()
        => new("Git returned malformed null-delimited path output.");
}
