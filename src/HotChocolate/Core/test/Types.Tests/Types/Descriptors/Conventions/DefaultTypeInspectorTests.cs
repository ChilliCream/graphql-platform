using System.Reflection;

namespace HotChocolate.Types.Descriptors;

public class DefaultTypeInspectorTests
{
    [Fact]
    public void Discover_Property_That_Returns_Object_And_Has_TypeAttribute()
    {
        // arrange
        var typeInspector = new DefaultTypeInspector();

        // act
        var discovered =
            typeInspector.GetMembers(typeof(ObjectPropWithTypeAttribute)).ToArray();

        // assert
        Assert.Collection(discovered,
            p => Assert.Equal("ShouldBeFound", p.Name));
    }

    [Fact]
    public void Discover_Property_That_Returns_Object_And_Has_DescriptorAttribute()
    {
        // arrange
        var typeInspector = new DefaultTypeInspector();

        // act
        var discovered =
            typeInspector.GetMembers(typeof(ObjectPropWithDescriptorAttribute)).ToArray();

        // assert
        Assert.Collection(discovered,
            p => Assert.Equal("ShouldBeFound", p.Name));
    }

    [Fact]
    public void Discover_Method_That_Returns_Object_And_Has_TypeAttribute()
    {
        // arrange
        var typeInspector = new DefaultTypeInspector();

        // act
        var discovered =
            typeInspector.GetMembers(typeof(ObjectMethodWithTypeAttribute)).ToArray();

        // assert
        Assert.Collection(discovered,
            p => Assert.Equal("ShouldBeFound", p.Name));
    }

    [Fact]
    public void Discover_Method_That_Returns_Object_And_Has_DescriptorAttribute()
    {
        // arrange
        var typeInspector = new DefaultTypeInspector();

        // act
        var discovered =
            typeInspector.GetMembers(typeof(ObjectMethodWithDescriptorAttribute)).ToArray();

        // assert
        Assert.Collection(discovered,
            p => Assert.Equal("ShouldBeFound", p.Name));
    }

    [Fact]
    public void Discover_Method_With_Object_Parameter_And_Has_TypeAttribute()
    {
        // arrange
        var typeInspector = new DefaultTypeInspector();

        // act
        var discovered =
            typeInspector.GetMembers(
                typeof(MethodAndObjectParameterWithTypeAttribute)).ToArray();

        // assert
        Assert.Collection(discovered,
            p => Assert.Equal("ShouldBeFound", p.Name));
    }

    [Fact]
    public void Discover_Method_With_Object_Parameter_And_Has_DescriptorAttribute()
    {
        // arrange
        var typeInspector = new DefaultTypeInspector();

        // act
        var discovered =
            typeInspector.GetMembers(
                typeof(MethodAndObjectParameterWithDescriptorAttribute)).ToArray();

        // assert
        Assert.Collection(discovered,
            p => Assert.Equal("ShouldBeFound", p.Name));
    }

    [Fact]
    public void Discover_Method_That_Returns_TaskObject_And_Has_TypeAttribute()
    {
        // arrange
        var typeInspector = new DefaultTypeInspector();

        // act
        var discovered =
            typeInspector.GetMembers(typeof(TaskObjectMethodWithTypeAttribute)).ToArray();

        // assert
        Assert.Collection(discovered,
            p => Assert.Equal("ShouldBeFound", p.Name));
    }

    [Fact]
    public void Discover_Method_That_Returns_TaskObject_And_Has_DescriptorAttribute()
    {
        // arrange
        var typeInspector = new DefaultTypeInspector();

        // act
        var discovered =
            typeInspector.GetMembers(typeof(TaskObjectMethodWithDescriptorAttribute)).ToArray();

        // assert
        Assert.Collection(discovered,
            p => Assert.Equal("ShouldBeFound", p.Name));
    }

    [Fact]
    public void Discover_Method_That_Returns_ValueTaskObject_And_Has_TypeAttribute()
    {
        // arrange
        var typeInspector = new DefaultTypeInspector();

        // act
        var discovered =
            typeInspector.GetMembers(typeof(ValueTaskObjectMethodWithTypeAttribute)).ToArray();

        // assert
        Assert.Collection(discovered,
            p => Assert.Equal("ShouldBeFound", p.Name));
    }

    [Fact]
    public void Discover_Method_That_Returns_ValueTaskObject_And_Has_DescriptorAttribute()
    {
        // arrange
        var typeInspector = new DefaultTypeInspector();

        // act
        var discovered =
            typeInspector.GetMembers(
                    typeof(ValueTaskObjectMethodWithDescriptorAttribute))
                .ToArray();

        // assert
        Assert.Collection(discovered,
            p => Assert.Equal("ShouldBeFound", p.Name));
    }

    [Fact]
    public void GetReturnTypeRef_FromMethod()
    {
        // arrange
        var method = typeof(Foo).GetMethod(nameof(Foo.Bar));
        var typeInspector = new DefaultTypeInspector();

        // act
        var typeReference =
            typeInspector.GetReturnTypeRef(method!, TypeContext.Output);

        // assert
        var extTypeRef = Assert.IsType<ExtendedTypeReference>(typeReference);
        Assert.Equal("List<String!>!", extTypeRef.Type.ToString());
        Assert.Equal(TypeContext.Output, typeReference.Context);
        Assert.Null(typeReference.Scope);
    }

    [Fact]
    public void GetReturnTypeRef_FromMethod_With_Scope()
    {
        // arrange
        var typeInspector = new DefaultTypeInspector();
        var method = typeof(Foo).GetMethod(nameof(Foo.Bar));
        // act
        var typeReference =
            typeInspector.GetReturnTypeRef(method!, TypeContext.Output, "abc");

        // assert
        var extTypeRef = Assert.IsType<ExtendedTypeReference>(typeReference);
        Assert.Equal("List<String!>!", extTypeRef.Type.ToString());
        Assert.Equal(TypeContext.Output, typeReference.Context);
        Assert.Equal("abc", typeReference.Scope);
    }

    [Fact]
    public void GetReturnTypeRef_FromMethod_Member_Is_Null()
    {
        // arrange
        var typeInspector = new DefaultTypeInspector();

        // act
        void Action() =>
            typeInspector.GetReturnTypeRef(null!, TypeContext.Output);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void GetReturnType_FromMethod()
    {
        // arrange
        var method = typeof(Foo).GetMethod(nameof(Foo.Bar));
        var typeInspector = new DefaultTypeInspector();

        // act
        var extendedType = typeInspector.GetReturnType(method!);

        // assert
        Assert.Equal("List<String!>!", extendedType.ToString());
    }

    [Fact]
    public void GetReturnType_FromMethod_Member_Is_Null()
    {
        // arrange
        var typeInspector = new DefaultTypeInspector();

        // act
        void Action() => typeInspector.GetReturnType(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void GetArgumentTypeRef()
    {
        // arrange
        var parameter = typeof(Foo).GetMethod(nameof(Foo.Baz))!.GetParameters()[0];
        var typeInspector = new DefaultTypeInspector();

        // act
        var typeReference = typeInspector.GetArgumentTypeRef(parameter!);

        // assert
        var extTypeRef = Assert.IsType<ExtendedTypeReference>(typeReference);
        Assert.Equal("String!", extTypeRef.Type.ToString());
        Assert.Equal(TypeContext.Input, typeReference.Context);
        Assert.Null(typeReference.Scope);
    }

    [Fact]
    public void GetArgumentTypeRef_With_Scope()
    {
        // arrange
        var parameter = typeof(Foo).GetMethod(nameof(Foo.Baz))!.GetParameters()[0];
        var typeInspector = new DefaultTypeInspector();

        // act
        var typeReference = typeInspector.GetArgumentTypeRef(parameter!, "abc");

        // assert
        var extTypeRef = Assert.IsType<ExtendedTypeReference>(typeReference);
        Assert.Equal("String!", extTypeRef.Type.ToString());
        Assert.Equal(TypeContext.Input, typeReference.Context);
        Assert.Equal("abc", typeReference.Scope);
    }

    [Fact]
    public void GetMemberType_With_SyntaxTypeRef()
    {
        // arrange
        var property =
            typeof(ObjectPropWithSyntaxType)
                .GetProperty(nameof(ObjectPropWithSyntaxType.ShouldBeFound))!;
        var typeInspector = new DefaultTypeInspector();

        // act
        var typeReference = typeInspector.GetReturnTypeRef(property!);

        // assert
        var extTypeRef = Assert.IsType<SyntaxTypeReference>(typeReference);
        Assert.Equal("[String]", extTypeRef.Type.ToString());
        Assert.Equal(TypeContext.None, typeReference.Context);
        Assert.Null(typeReference.Scope);
    }

    [Fact]
    public void GetArgumentTypeRef_Parameter_Is_Null()
    {
        // arrange
        var typeInspector = new DefaultTypeInspector();

        // act
        void Action() => typeInspector.GetArgumentTypeRef(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void GetArgumentType()
    {
        // arrange
        var parameter = typeof(Foo).GetMethod(nameof(Foo.Baz))!.GetParameters()[0];
        var typeInspector = new DefaultTypeInspector();

        // act
        var extendedType = typeInspector.GetArgumentType(parameter!);

        // assert
        Assert.Equal("String!", extendedType.ToString());
    }

    [Fact]
    public void GetArgumentType_Parameter_Is_Null()
    {
        // arrange
        var typeInspector = new DefaultTypeInspector();

        // act
        void Action() => typeInspector.GetArgumentType(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void GetTypeRef()
    {
        // arrange
        var type = typeof(Foo);
        var typeInspector = new DefaultTypeInspector();

        // act
        var typeReference =
            typeInspector.GetTypeRef(type!, TypeContext.Output);

        // assert
        Assert.Equal("Foo", typeReference.Type.ToString());
        Assert.Equal(TypeContext.Output, typeReference.Context);
        Assert.Null(typeReference.Scope);
    }

    [Fact]
    public void GetTypeRef_With_Scope()
    {
        // arrange
        var type = typeof(Foo);
        var typeInspector = new DefaultTypeInspector();

        // act
        var typeReference =
            typeInspector.GetTypeRef(type!, TypeContext.Output, "abc");

        // assert
        Assert.Equal("Foo", typeReference.Type.ToString());
        Assert.Equal(TypeContext.Output, typeReference.Context);
        Assert.Equal("abc", typeReference.Scope);
    }

    [Fact]
    public void GetTypeRef_Type_Is_Null()
    {
        // arrange
        var typeInspector = new DefaultTypeInspector();

        // act
        void Action() => typeInspector.GetTypeRef(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void GetType_Type_Is_Foo()
    {
        // arrange
        var type = typeof(Foo);
        var typeInspector = new DefaultTypeInspector();

        // act
        var extendedType = typeInspector.GetType(type!);

        // assert
        Assert.Equal("Foo", extendedType.ToString());
    }

    [Fact]
    public void GetType_Type_Is_Null()
    {
        // arrange
        var typeInspector = new DefaultTypeInspector();

        // act
        void Action() => typeInspector.GetType(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void GetType2_Rewrite_Foo_To_NonNullFoo()
    {
        // arrange
        var type = typeof(Foo);
        var typeInspector = new DefaultTypeInspector();

        // act
        var extendedType = typeInspector.GetType(type!, false);

        // assert
        Assert.Equal("Foo!", extendedType.ToString());
    }

    [Fact]
    public void GetType2_Type_Is_Null()
    {
        // arrange
        var typeInspector = new DefaultTypeInspector();

        // act
        void Action() => typeInspector.GetType(null!, false);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void GetType2_Nullable_Is_Null()
    {
        // arrange
        var typeInspector = new DefaultTypeInspector();

        // act
        void Action() => typeInspector.GetType(typeof(Foo), default!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void GetEnumValues()
    {
        // arrange
        var typeInspector = new DefaultTypeInspector();

        // act
        var values = typeInspector.GetEnumValues(typeof(BarEnum));

        // assert
        Assert.Collection(
            values,
            t => Assert.Equal(BarEnum.Bar, t),
            t => Assert.Equal(BarEnum.Baz, t));
    }

    [Fact]
    public void GetEnumValues_Type_Is_Null()
    {
        // arrange
        var typeInspector = new DefaultTypeInspector();

        // act
        void Action() => typeInspector.GetEnumValues(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void GetEnumValueMember()
    {
        // arrange
        var typeInspector = new DefaultTypeInspector();

        // act
        var valueMember = typeInspector.GetEnumValueMember(BarEnum.Bar);

        // assert
        Assert.Equal("Bar", valueMember!.Name);
    }

    [Fact]
    public void GetEnumValueMember_Type_Is_Null()
    {
        // arrange
        var typeInspector = new DefaultTypeInspector();

        // act
        void Action() => typeInspector.GetEnumValueMember(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void ExtractNamedType_From_Non_SchemaType()
    {
        // arrange
        var typeInspector = new DefaultTypeInspector();

        // act
        var type = typeInspector.ExtractNamedType(typeof(List<string>));

        // assert
        Assert.Equal(typeof(List<string>), type);
    }

    [Fact]
    public void ExtractNamedType_From_SchemaType()
    {
        // arrange
        var typeInspector = new DefaultTypeInspector();

        // act
        var type = typeInspector.ExtractNamedType(typeof(ListType<StringType>));

        // assert
        Assert.Equal(typeof(StringType), type);
    }

    [Fact]
    public void ExtractNamedType_Type_Is_Null()
    {
        // arrange
        var typeInspector = new DefaultTypeInspector();

        // act
        void Action() => typeInspector.ExtractNamedType(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void IsSchemaType_From_Non_SchemaType()
    {
        // arrange
        var typeInspector = new DefaultTypeInspector();

        // act
        var isSchemaType = typeInspector.IsSchemaType(typeof(List<string>));

        // assert
        Assert.False(isSchemaType);
    }

    [Fact]
    public void IsSchemaType_From_SchemaType()
    {
        // arrange
        var typeInspector = new DefaultTypeInspector();

        // act
        var isSchemaType = typeInspector.IsSchemaType(typeof(ListType<StringType>));

        // assert
        Assert.True(isSchemaType);
    }

    [Fact]
    public void IsSchemaType_Type_Is_Null()
    {
        // arrange
        var typeInspector = new DefaultTypeInspector();

        // act
        void Action() => typeInspector.IsSchemaType(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void CollectNullability_Nullable_StringType()
    {
        // arrange
        var typeInspector = new DefaultTypeInspector();
        var extendedType = typeInspector.GetType(typeof(StringType));

        // act
        var nullability = typeInspector.CollectNullability(extendedType);

        // assert
        Assert.Collection(nullability, item => Assert.True(item));
    }

    [Fact]
    public void CollectNullability_NonNull_StringType()
    {
        // arrange
        var typeInspector = new DefaultTypeInspector();
        var extendedType = typeInspector.GetType(typeof(NonNullType<StringType>));

        // act
        var nullability = typeInspector.CollectNullability(extendedType);

        // assert
        Assert.Collection(nullability, item => Assert.False(item));
    }

    [Fact]
    public void CollectNullability_List_NonNull_StringType()
    {
        // arrange
        var typeInspector = new DefaultTypeInspector();
        var extendedType = typeInspector.GetType(
            typeof(ListType<NonNullType<StringType>>));

        // act
        var nullability = typeInspector.CollectNullability(extendedType);

        // assert
        Assert.Collection(nullability, Assert.True, Assert.False);
    }

    [Fact]
    public void EnsureOnlyThingsWeUnderstandAreInferred()
    {
        // arrange
        var typeInspector = new DefaultTypeInspector();

        // act
        var members = new List<MemberInfo>();
        foreach (var member in typeInspector.GetMembers(typeof(DoNotInfer)))
        {
            members.Add(member);
        }

        // assert
        Assert.Collection(
            members.OrderBy(t => t.Name),
            member => Assert.Equal("AsyncEnumerable", member.Name),
            member => Assert.Equal("DoInfer", member.Name));
    }

    public class ObjectPropWithTypeAttribute
    {
        public object ShouldNotBeFound { get; }

        [GraphQLType(typeof(StringType))]
        public object ShouldBeFound { get; }
    }

    public class ObjectPropWithDescriptorAttribute
    {
        public object ShouldNotBeFound { get; }

        [SomeAttribute]
        public object ShouldBeFound { get; }
    }

    public class ObjectPropWithSyntaxType
    {
        [GraphQLType("[String]")]
        public object ShouldBeFound { get; }
    }

    public class ObjectMethodWithTypeAttribute
    {
        public object ShouldNotBeFound() => null;

        [GraphQLType(typeof(StringType))]
        public object ShouldBeFound() => null;
    }

    public class ObjectMethodWithDescriptorAttribute
    {
        public object ShouldNotBeFound() => null;

        [SomeAttribute]
        public object ShouldBeFound() => null;
    }

    public class MethodAndObjectParameterWithTypeAttribute
    {
        public string ShouldNotBeFound(
            object o) => null;

        public string ShouldBeFound(
            [GraphQLType(typeof(StringType))]
            object o) => null;
    }

    public class MethodAndObjectParameterWithDescriptorAttribute
    {
        public string ShouldNotBeFound(
            object o) => null;

        public string ShouldBeFound(
            [Some]
            object o) => null;
    }

    public class TaskObjectMethodWithTypeAttribute
    {
        public Task<object> ShouldNotBeFound() => null!;

        [GraphQLType(typeof(StringType))]
        public Task<object> ShouldBeFound() => null!;
    }

    public class TaskObjectMethodWithDescriptorAttribute
    {
        public Task<object> ShouldNotBeFound() => null!;

        [Some]
        public Task<object> ShouldBeFound() => null!;
    }

    public class ValueTaskObjectMethodWithTypeAttribute
    {
        public ValueTask<object> ShouldNotBeFound() => default;

        [GraphQLType(typeof(StringType))]
        public ValueTask<object> ShouldBeFound() => default;
    }

    public class ValueTaskObjectMethodWithDescriptorAttribute
    {
        public ValueTask<object> ShouldNotBeFound() => default;

        [SomeAttribute]
        public ValueTask<object> ShouldBeFound() => default;
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public sealed class SomeAttribute : DescriptorAttribute
    {
        protected internal override void TryConfigure(
            IDescriptorContext context,
            IDescriptor descriptor,
            ICustomAttributeProvider element)
        {
            throw new NotImplementedException();
        }
    }

#nullable enable

    public class Foo
    {
        public List<string> Bar() => throw new NotImplementedException();

        public List<string> Baz(string s) => throw new NotImplementedException();
    }

#nullable restore

    public enum BarEnum
    {
        Bar,
        Baz,
    }

    public class DoNotInfer
    {
        private string _s = "";

        public string DoInfer() => "abc";

        public void ReturnsVoid() { }

        public object ObjectProp { get; } = null;

        public string ByRefParameter(ref string s) => s;

        public string ByRefInParameter(in string s) => s;

        public string OutParameter(out string s)
        {
            s = "";
            return "";
        }

        public ref string ByRefReturn()
        {
            return ref _s;
        }

        public string TypeParameter(Type type) => "abc";

        public string TypeParameter(ParameterInfo type) => "abc";

        public string TypeParameter(MemberInfo type) => "abc";

        public string ActionParam(Action action) => "abc";

        public Type GetMyType() => typeof(Foo);

        public IAsyncResult GetSomeAsyncResult() => null;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async void GetAsyncVoid() { }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

        public string RefStruct(ReadOnlySpan<byte> bytes) => "";

        public Action Action { get; }

        public async IAsyncEnumerable<string> AsyncEnumerable()
        {
            await Task.Delay(10);
            yield return "abc";
        }
    }
}
