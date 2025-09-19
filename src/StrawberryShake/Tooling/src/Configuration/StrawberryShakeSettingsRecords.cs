namespace StrawberryShake.Tools.Configuration;

/// <summary>
/// This settings class defines which parts shall be generated as records.
/// </summary>
public class StrawberryShakeSettingsRecords
{
    /// <summary>
    /// Defines if the generator shall generate records for input types.
    /// </summary>
    public bool Inputs { get; set; }

    /// <summary>
    /// Defines if the generator shall generate records for entities.
    /// </summary>
    public bool Entities { get; set; }
}
