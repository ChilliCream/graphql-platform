using System;
using System.Collections.Generic;
using System.Text;

namespace HotChocolate.Types.Descriptors
{
    internal class ConventionRecord
    {
        public Type Convention { get; }
        public CreateConvention CreateConvention { get; }

        public ConventionRecord(Type convention, CreateConvention createConention)
        {
            Convention = convention;
            CreateConvention = createConention;
        }
    }
}
