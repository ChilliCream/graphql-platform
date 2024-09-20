namespace HotChocolate.Types.Analyzers.Models;

[Flags]
public enum ModuleOptions
{
    Default = RegisterDataLoader | RegisterTypes,
    RegisterTypes = 1,
    RegisterDataLoader = 2,
    Disabled = 4
}
