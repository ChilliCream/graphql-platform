namespace HotChocolate.Types.Spatial
{
    internal static class GeoJsonDescriptorExtensions
    {
        public static void GeoJsonName<T>(
            this IObjectTypeDescriptor<T> descriptor,
            string name) =>
            descriptor.Name(name.Replace("Json", "JSON"));

        public static void GeoJsonName<T>(
            this IInputObjectTypeDescriptor<T> descriptor,
            string name) =>
            descriptor.Name(name.Replace("Json", "JSON"));

        public static void GeoJsonName(
            this IInterfaceTypeDescriptor descriptor,
            string name) =>
            descriptor.Name(name.Replace("Json", "JSON"));
    }
}
