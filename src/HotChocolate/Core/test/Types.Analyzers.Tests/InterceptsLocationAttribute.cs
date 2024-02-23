#pragma warning disable CS9113 // Parameter is unread.
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    sealed class InterceptsLocationAttribute(string filePath, int line, int column) : Attribute;
}
#pragma warning restore CS9113 // Parameter is unread.
