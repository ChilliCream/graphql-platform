namespace ChilliCream.Nitro.Client.Stages;

public sealed record StageUpdateModel(string Name, string DisplayName, IReadOnlyList<string> AfterStages);
