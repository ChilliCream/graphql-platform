namespace ChilliCream.Hosting.CommandLine;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class HostCommandModuleAttribute<T> : Attribute where T : IHostCommandModule;
