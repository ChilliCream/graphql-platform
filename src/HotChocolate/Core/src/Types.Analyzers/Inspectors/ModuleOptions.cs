namespace HotChocolate.Types.Analyzers.Inspectors;

[Flags]
public enum ModuleOptions
{
    Default = RegisterDataLoader | RegisterTypes,
    RegisterTypes = 1,
    RegisterDataLoader = 2
}
