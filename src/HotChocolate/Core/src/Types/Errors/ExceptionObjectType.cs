using System;
using HotChocolate.Types;

namespace HotChocolate.Types.Errors
{
    internal class ExceptionObjectType<T> : ObjectType<T> where T : Exception
    {
        protected override void Configure(IObjectTypeDescriptor<T> descriptor)
        {
            descriptor.Ignore(x => x.Data);
            descriptor.Ignore(x => x.Source);
            descriptor.Ignore(x => x.HelpLink);
            descriptor.Ignore(x => x.HResult);
            descriptor.Ignore(x => x.InnerException);
            descriptor.Ignore(x => x.StackTrace);
            descriptor.Ignore(x => x.TargetSite);
            descriptor.Ignore(x => x.GetBaseException());
            descriptor.Field(x => x.Message).Type<NonNullType<StringType>>();
            descriptor.Implements<ErrorInterfaceType>();
        }
    }
}
