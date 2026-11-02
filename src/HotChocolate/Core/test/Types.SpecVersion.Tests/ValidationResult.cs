namespace HotChocolate.Types.SpecVersion;

public sealed record ValidationResult(bool IsSuccess, string StandardOutput, string StandardError);
