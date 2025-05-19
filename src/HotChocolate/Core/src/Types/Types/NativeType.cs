namespace HotChocolate.Types;

/// <summary>
/// Internal helper class to mark native types within a GraphQL context
/// e.g. <c><![CDATA[NonNullType<NativeType<String>>]]></c>
/// </summary>
// ReSharper disable once UnusedTypeParameter
internal class NativeType<T> : FluentWrapperType;
