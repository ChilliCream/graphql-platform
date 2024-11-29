namespace HotChocolate.Types;

internal sealed class ExceptionObjectType<T> : ObjectType<T> where T : Exception
{
    protected override void Configure(IObjectTypeDescriptor<T> descriptor)
    {
        descriptor.Name(GetNameFromException());
        descriptor.Ignore(x => x.Data);
        descriptor.Ignore(x => x.Source);
        descriptor.Ignore(x => x.HelpLink);
        descriptor.Ignore(x => x.HResult);
        descriptor.Ignore(x => x.InnerException);
        descriptor.Ignore(x => x.StackTrace);
        descriptor.Ignore(x => x.TargetSite);
        descriptor.Ignore(x => x.GetBaseException());
        descriptor.Field(x => x.Message).Type<NonNullType<StringType>>();
        descriptor.Extend().Definition.ContextData.MarkAsError();
        descriptor.BindFieldsImplicitly();
    }

    private static string GetNameFromException()
    {
        var name = typeof(T).Name;
        const string exceptionSuffix = nameof(Exception);

        return name.EndsWith(exceptionSuffix)
            ? $"{name.Substring(0, name.Length - exceptionSuffix.Length)}Error"
            : name;
    }
}
