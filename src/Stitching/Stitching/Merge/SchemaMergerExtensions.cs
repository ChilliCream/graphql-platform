using System;
using System.Linq;
using System.Reflection;

namespace HotChocolate.Stitching
{
    public static class SchemaMergerExtensions
    {
        public static ISchemaMerger AddHandler<T>(
            this ISchemaMerger merger)
            where T : ITypeMergeHanlder
        {
            if (merger == null)
            {
                throw new System.ArgumentNullException(nameof(merger));
            }

            merger.AddMergeHandler(CreateHandler<T>());

            return merger;
        }

        internal static MergeTypeHandler CreateHandler<T>()
            where T : ITypeMergeHanlder
        {
            ConstructorInfo constructor = typeof(T).GetTypeInfo()
                .DeclaredConstructors.SingleOrDefault(c =>
                {
                    ParameterInfo[] parameters = c.GetParameters();
                    return parameters.Length == 1
                        && parameters[0].ParameterType ==
                            typeof(MergeTypeDelegate);
                });

            if (constructor == null)
            {
                throw new ArgumentException(
                    "A type merge handler has to have one constructore " +
                    "that has only one parameter of the type " +
                    "MergeTypeDelegate.");
            }

            return next =>
            {
                ITypeMergeHanlder handler = (ITypeMergeHanlder)constructor
                    .Invoke(new object[] { next });
                return handler.Merge;
            };
        }
    }
}
