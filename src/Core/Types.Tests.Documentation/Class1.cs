using System;

namespace HotChocolate.Types.Descriptors
{
    public class WithMultilineXmlDoc
    {
        /// <summary>
        /// Query and manages users.
        ///
        /// Please note:
        /// * Users ...
        /// * Users ...
        ///     * Users ...
        ///     * Users ...
        ///
        /// You need one of the following role: Owner,
        /// Editor, use XYZ to manage permissions.
        /// </summary>
        public string Foo { get; set; }
    }

    public class WithSeeTagInXmlDoc
    {
        /// <summary>
        /// <see langword="null"/> for the default <see cref="Record"/>.
        /// See <see cref="Record">this</see> and
        /// <see href="https://github.com/rsuter/njsonschema">this</see> at
        /// <see href="https://github.com/rsuter/njsonschema"/>.
        /// </summary>
        public string Foo { get; set; }
    }

    public class WithGenericTagsInXmlDoc
    {
        /// <summary>These <c>are</c> <strong>some</strong> tags.</summary>
        public string Foo { get; set; }
    }

    /// <summary>
    /// I am the most base class.
    /// </summary>
    public abstract class BaseBaseClass
    {
        /// <summary>Summary of foo.</summary>
        public abstract string Foo { get; }

        /// <summary>Method doc.</summary>
        /// <param name="baz">Parameter details.</param>
        public abstract void Bar(string baz);
    }

    public abstract class BaseClass : BaseBaseClass
    {
        /// <inheritdoc />
        public override string Foo { get; }

        /// <inheritdoc />
        public override void Bar(string baz) { }
    }

    public class ClassWithInheritdoc : BaseClass
    {
        /// <inheritdoc />
        public override string Foo { get; }

        /// <inheritdoc />
        public override void Bar(string baz) { }
    }

    /// <summary>
    /// I am an interface.
    /// </summary>
    public interface IBaseBaseInterface
    {
        /// <summary>Property summary.</summary>
        string Foo { get; }

        /// <summary>Method summary.</summary>
        /// <param name="baz">Parameter summary.</param>
        void Bar(string baz);
    }

    public interface IBaseInterface : IBaseBaseInterface
    {
    }

    public class ClassWithInheritdocOnInterface : IBaseInterface
    {
        /// <inheritdoc />
        public string Foo { get; }

        /// <inheritdoc />
        public void Bar(string baz) { }
    }

    public class ClassWithInterfaceAndCustomSummaries : IBaseInterface
    {
        /// <summary>
        /// I am my own property.
        /// </summary>
        public string Foo { get; }

        /// <summary>
        /// I am my own method.
        /// </summary>
        /// <param name="baz">I am my own parameter.</param>
        public void Bar(string baz) { }
    }

    /// <summary>
    /// I am a test class.
    /// </summary>
    public class ClassWithSummary
    {
    }
}
