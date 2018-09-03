public class SkipDirective
{
    public bool If { get; set; }

}



public class TestDirective
{

}

public class Directive
{
    public string Name { get; }
    public IReadOnlyDictionary<string, IValueNode> Arguments { get; }
}

public class TestDirectiveType<TestDirective>
    : DirectiveType<TestDirective>
{
    protected override void Configure(IDirectiveDescriptor desc)
    {


    }
}

public class MyValidationDirective
    : Directive
{
    protected override void Configure(IDirectiveDescriptor descriptor)
    {
        descriptor.Middleware<ValidatorMiddleware>();
    }
}
