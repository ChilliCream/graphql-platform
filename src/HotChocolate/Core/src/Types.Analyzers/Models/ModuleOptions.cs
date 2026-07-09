namespace HotChocolate.Types.Analyzers.Models;

[Flags]
public enum ModuleOptions
{
    Default = RegisterDataLoader | RegisterTypes,
    RegisterTypes = 1,
    RegisterDataLoader = 2,
    IncludeInternalMembers = 4,
    DisableXmlDocumentation = 8,
    Disabled = 16
}
