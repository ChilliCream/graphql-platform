using System;
using System.Linq;
using System.Reflection;
using HotChocolate.Stitching.Properties;

namespace HotChocolate.Stitching.Merge
{
    public static class SchemaMergerExtensions
    {
        public static ISchemaMerger AddMergeHandler<T>(
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
                    Resources.SchemaMergerExtensions_NoValidConstructor);
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
