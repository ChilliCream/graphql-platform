using System.Collections.Generic;

namespace HotChocolate.Execution.Processing
{
    public sealed class ResultMapList
        : List<IResultMap?>
        , IResultMapList
        , IHasResultDataParent
    {
        public IResultData? Parent { get; set; }

        IResultData? IHasResultDataParent.Parent { get => Parent; set => Parent = value; }

        /// <summary>
        /// Defines if the elements of this list are nullable.
        /// </summary>
        public bool IsNullable { get; set; }
    }
}
