using System;
using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Stitching.Merge.Handlers
{
    public abstract class TypeMergeHanlderBase<T>
        : ITypeMergeHandler
        where T : ITypeInfo
    {
        private readonly MergeTypeRuleDelegate _next;

        protected TypeMergeHanlderBase(MergeTypeRuleDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public void Merge(
            ISchemaMergeContext context,
            IReadOnlyList<ITypeInfo> types)
        {
            if (types.OfType<T>().Any())
            {
                var notMerged = types.OfType<T>().ToList();
                bool hasLeftovers = types.Count > notMerged.Count;

                while (notMerged.Count > 0)
                {
                    MergeNextType(context, notMerged);
                }

                if (hasLeftovers)
                {
                    _next.Invoke(context, types.NotOfType<T>());
                }
            }
            else
            {
                _next.Invoke(context, types);
            }
        }

        private void MergeNextType(
            ISchemaMergeContext context,
            List<T> notMerged)
        {
            T left = notMerged[0];

            var readyToMerge = new List<T>();
            readyToMerge.Add(left);

            for (int i = 1; i < notMerged.Count; i++)
            {
                if (CanBeMerged(left, notMerged[i]))
                {
                    readyToMerge.Add(notMerged[i]);
                }
            }

            NameString newTypeName =
                TypeMergeHelpers.CreateName<T>(
                    context, readyToMerge);

            MergeTypes(context, readyToMerge, newTypeName);
            notMerged.RemoveAll(readyToMerge.Contains);
        }

        protected abstract bool CanBeMerged(T left, T right);

        protected abstract void MergeTypes(
            ISchemaMergeContext context,
            IReadOnlyList<T> types,
            NameString newTypeName);
    }
}
