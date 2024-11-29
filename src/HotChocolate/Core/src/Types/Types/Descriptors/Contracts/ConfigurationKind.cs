namespace HotChocolate.Types;

/// <summary>
/// This enum defines the events on which configurations can be applied.
/// </summary>
public enum ApplyConfigurationOn
{
    /// <summary>
    /// Before the type is created.
    /// </summary>
    Create,

    /// <summary>
    /// Before the types name is completed.
    /// </summary>
    BeforeNaming,

    /// <summary>
    /// After the types name is completed.
    /// </summary>
    AfterNaming,

    /// <summary>
    /// Before the type is completed.
    /// </summary>
    BeforeCompletion,

    /// <summary>
    /// After the type is completed.
    /// </summary>
    AfterCompletion,
}
