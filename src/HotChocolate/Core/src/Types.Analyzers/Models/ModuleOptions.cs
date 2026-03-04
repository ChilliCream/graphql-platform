namespace HotChocolate.Types.Analyzers.Models;

[Flags]
public enum ModuleOptions
{
    Default = RegisterDataLoader | RegisterTypes,
    RegisterTypes = 1,
    RegisterDataLoader = 2,
    IncludeInternalMembers = 4,
    Disabled = 8
}
