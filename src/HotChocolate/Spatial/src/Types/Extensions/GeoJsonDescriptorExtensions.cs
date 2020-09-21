namespace HotChocolate.Types.Spatial
{
    internal static class GeoJsonDescriptorExtensions
    {
        public static void GeoJsonName<T>(
            this IObjectTypeDescriptor<T> descriptor,
            string name) =>
            descriptor.Name(name.GeoJsonName());

        public static void GeoJsonName<T>(
            this IInputObjectTypeDescriptor<T> descriptor,
            string name) =>
            descriptor.Name(name.GeoJsonName());

        public static void GeoJsonName<T>(
            this IEnumTypeDescriptor<T> descriptor,
            string name) =>
            descriptor.Name(name.GeoJsonName());

        public static void GeoJsonName(
            this IInterfaceTypeDescriptor descriptor,
            string name) =>
            descriptor.Name(name.GeoJsonName());

        private static string GeoJsonName(this string name) => name.Replace("Json", "JSON");
    }
}
