using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using HotChocolate.Utilities;
using Microsoft.Extensions.ObjectPool;
using Xunit.Abstractions;
using IOPath = System.IO.Path;

#pragma warning disable

namespace HotChocolate.Types.Descriptors;

public class XmlDocumentProviderBenchmarks
{
    private readonly ITestOutputHelper _testOutputHelper;

    public XmlDocumentProviderBenchmarks(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact(Skip = "Run manually when performance regression testing is required")]
    //[Fact]
    public void RunBenchmarks()
    {
        var config = ManualConfig.Create(DefaultConfig.Instance)
            .AddDiagnoser(MemoryDiagnoser.Default)
            .AddJob(Job.ShortRun)
            .AddExporter(MarkdownExporter.GitHub);

        var summary = BenchmarkRunner.Run(typeof(Bench), config);

        var files = MarkdownExporter.GitHub.ExportToFiles(summary, NullLogger.Instance);

        _testOutputHelper.WriteLine($"Benchmark report saved to: {string.Join(" ,", files)}");
        _testOutputHelper.WriteLine(string.Join("\n", summary.ValidationErrors));
    }

    public class Bench
    {
        private static readonly XmlDocumentationProvider s_documentationProvider = new XmlDocumentationProvider(
            new XmlDocumentationResolver(),
            new DefaultObjectPoolProvider().CreateStringBuilderPool());

        private static readonly OldXmlDocumentationProvider s_oldDocumentationProvider = new OldXmlDocumentationProvider(
            new OldXmlDocumentationFileResolver(),
            new DefaultObjectPoolProvider().CreateStringBuilderPool());

        // Example parameterization
        [Params(1, 10, 100)] public int N { get; set; }

        [Benchmark]
        public void When_xml_doc_is_missing_then_description_is_empty()
        {
            for (int i = 0; i < N; i++)
            {
                s_documentationProvider.GetDescription(typeof(Point));
            }
        }

        [Benchmark]
        public void When_xml_doc_is_missing_then_description_is_empty_old()
        {
            for (int i = 0; i < N; i++)
            {
                s_oldDocumentationProvider.GetDescription(typeof(Point));
            }
        }

        [Benchmark]
        public void When_xml_doc_with_multiple_breaks_is_read_then_they_are_not_stripped_away()
        {
            for (int i = 0; i < N; i++)
            {
                s_documentationProvider.GetDescription(
                    typeof(WithMultilineXmlDoc)
                        .GetProperty(nameof(WithMultilineXmlDoc.Foo))!);
            }
        }

        [Benchmark]
        public void When_xml_doc_with_multiple_breaks_is_read_then_they_are_not_stripped_away_old()
        {
            for (int i = 0; i < N; i++)
            {
                s_oldDocumentationProvider.GetDescription(
                    typeof(WithMultilineXmlDoc)
                        .GetProperty(nameof(WithMultilineXmlDoc.Foo))!);
            }
        }

        [Benchmark]
        public void When_description_has_see_tag_then_it_is_converted()
        {
            for (int i = 0; i < N; i++)
            {
                // act
                s_documentationProvider.GetDescription(
                    typeof(WithSeeTagInXmlDoc)
                        .GetProperty(nameof(WithSeeTagInXmlDoc.Foo))!);
            }
        }

        [Benchmark]
        public void When_description_has_see_tag_then_it_is_converted_old()
        {
            for (int i = 0; i < N; i++)
            {
                // act
                s_oldDocumentationProvider.GetDescription(
                    typeof(WithSeeTagInXmlDoc)
                        .GetProperty(nameof(WithSeeTagInXmlDoc.Foo))!);
            }
        }

        [Benchmark]
        public void When_description_has_paramref_tag_then_it_is_converted()
        {
            for (int i = 0; i < N; i++)
            {
                s_documentationProvider.GetDescription(
                    typeof(WithParamrefTagInXmlDoc)
                        .GetMethod(nameof(WithParamrefTagInXmlDoc.Foo))!);
            }
        }

        [Benchmark]
        public void When_description_has_paramref_tag_then_it_is_converted_old()
        {
            for (int i = 0; i < N; i++)
            {
                s_oldDocumentationProvider.GetDescription(
                    typeof(WithParamrefTagInXmlDoc)
                        .GetMethod(nameof(WithParamrefTagInXmlDoc.Foo))!);
            }
        }

        [Benchmark]
        public void When_description_has_generic_tags_then_it_is_converted()
        {
            for (int i = 0; i < N; i++)
            {
                // act
                s_documentationProvider.GetDescription(
                    typeof(WithGenericTagsInXmlDoc)
                        .GetProperty(nameof(WithGenericTagsInXmlDoc.Foo))!);
            }
        }

        [Benchmark]
        public void When_description_has_generic_tags_then_it_is_converted_old()
        {
            for (int i = 0; i < N; i++)
            {
                // act
                s_oldDocumentationProvider.GetDescription(
                    typeof(WithGenericTagsInXmlDoc)
                        .GetProperty(nameof(WithGenericTagsInXmlDoc.Foo))!);
            }
        }

        [Benchmark]
        public void When_type_has_description_then_it_it_resolved()
        {
            for (int i = 0; i < N; i++)
            {
                // act
                s_documentationProvider.GetDescription(
                    typeof(BaseBaseClass));
            }
        }

        [Benchmark]
        public void When_type_has_description_then_it_it_resolved_old()
        {
            for (int i = 0; i < N; i++)
            {
                // act
                s_oldDocumentationProvider.GetDescription(
                    typeof(BaseBaseClass));
            }
        }

        [Benchmark]
        public void When_we_use_custom_documentation_files_they_are_correctly_loaded()
        {
            for (int i = 0; i < N; i++)
            {
                // act
                s_documentationProvider.GetDescription(
                    typeof(BaseBaseClass));
            }
        }

        [Benchmark]
        public void When_we_use_custom_documentation_files_they_are_correctly_loaded_old()
        {
            for (int i = 0; i < N; i++)
            {
                // act
                s_oldDocumentationProvider.GetDescription(
                    typeof(BaseBaseClass));
            }
        }

        [Benchmark]
        public void When_parameter_has_inheritdoc_then_it_is_resolved()
        {
            for (int i = 0; i < N; i++)
            {
                // act
                s_documentationProvider.GetDescription(
                    typeof(ClassWithInheritdoc)
                        .GetMethod(nameof(ClassWithInheritdoc.Bar))!
                        .GetParameters()
                        .Single(p => p.Name == "baz"));
            }
        }
        [Benchmark]
        public void When_parameter_has_inheritdoc_then_it_is_resolved_old()
        {
            for (int i = 0; i < N; i++)
            {
                // act
                s_oldDocumentationProvider.GetDescription(
                    typeof(ClassWithInheritdoc)
                        .GetMethod(nameof(ClassWithInheritdoc.Bar))!
                        .GetParameters()
                        .Single(p => p.Name == "baz"));
            }
        }

        [Benchmark]
        public void When_method_has_inheritdoc_then_it_is_resolved()
        {
            for (int i = 0; i < N; i++)
            {
                // act
                s_documentationProvider.GetDescription(
                    typeof(ClassWithInheritdoc)
                        .GetMethod(nameof(ClassWithInheritdoc.Bar))!);
            }
        }

        [Benchmark]
        public void When_method_has_inheritdoc_then_it_is_resolved_old()
        {
            for (int i = 0; i < N; i++)
            {
                // act
                s_oldDocumentationProvider.GetDescription(
                    typeof(ClassWithInheritdoc)
                        .GetMethod(nameof(ClassWithInheritdoc.Bar))!);
            }
        }

        [Benchmark]
        public void When_property_has_inheritdoc_then_it_is_resolved()
        {
            for (int i = 0; i < N; i++)
            {
                // act
                s_documentationProvider.GetDescription(
                    typeof(ClassWithInheritdoc)
                        .GetProperty(nameof(ClassWithInheritdoc.Foo))!);
            }
        }

        [Benchmark]
        public void When_property_has_inheritdoc_then_it_is_resolved_old()
        {
            for (int i = 0; i < N; i++)
            {
                // act
                s_oldDocumentationProvider.GetDescription(
                    typeof(ClassWithInheritdoc)
                        .GetProperty(nameof(ClassWithInheritdoc.Foo))!);
            }
        }

        [Benchmark]
        public void When_type_is_an_interface_then_description_is_resolved()
        {
            for (int i = 0; i < N; i++)
            {
                // act
                s_documentationProvider.GetDescription(
                    typeof(IBaseBaseInterface));
            }
        }
        [Benchmark]
        public void When_type_is_an_interface_then_description_is_resolved_old()
        {
            for (int i = 0; i < N; i++)
            {
                // act
                s_oldDocumentationProvider.GetDescription(
                    typeof(IBaseBaseInterface));
            }
        }

        [Benchmark]
        public void When_parameter_has_inheritdoc_on_interface_then_it_is_resolved()
        {
            for (int i = 0; i < N; i++)
            {
                // act
                s_documentationProvider.GetDescription(
                    typeof(ClassWithInheritdocOnInterface)
                        .GetMethod(nameof(ClassWithInheritdocOnInterface.Bar))!
                        .GetParameters()
                        .Single(p => p.Name == "baz"));
            }
        }

        [Benchmark]
        public void When_parameter_has_inheritdoc_on_interface_then_it_is_resolved_old()
        {
            for (int i = 0; i < N; i++)
            {
                // act
                s_oldDocumentationProvider.GetDescription(
                    typeof(ClassWithInheritdocOnInterface)
                        .GetMethod(nameof(ClassWithInheritdocOnInterface.Bar))!
                        .GetParameters()
                        .Single(p => p.Name == "baz"));
            }
        }


        [Benchmark]
        public void When_property_has_inheritdoc_on_interface_then_it_is_resolved()
        {
            for (int i = 0; i < N; i++)
            {
                // act
                s_documentationProvider.GetDescription(
                    typeof(ClassWithInheritdocOnInterface)
                        .GetProperty(nameof(ClassWithInheritdocOnInterface.Foo))!);
            }
        }

        [Benchmark]
        public void When_property_has_inheritdoc_on_interface_then_it_is_resolved_old()
        {
            for (int i = 0; i < N; i++)
            {
                // act
                s_oldDocumentationProvider.GetDescription(
                    typeof(ClassWithInheritdocOnInterface)
                        .GetProperty(nameof(ClassWithInheritdocOnInterface.Foo))!);
            }
        }

        [Benchmark]
        public void When_method_has_inheritdoc_then_on_interface_it_is_resolved()
        {
            for (int i = 0; i < N; i++)
            {
                // act
                s_documentationProvider.GetDescription(
                    typeof(ClassWithInheritdocOnInterface)
                        .GetMethod(nameof(ClassWithInheritdocOnInterface.Bar))!);
            }
        }

        [Benchmark]
        public void When_method_has_inheritdoc_then_on_interface_it_is_resolved_old()
        {
            for (int i = 0; i < N; i++)
            {
                // act
                s_oldDocumentationProvider.GetDescription(
                    typeof(ClassWithInheritdocOnInterface)
                        .GetMethod(nameof(ClassWithInheritdocOnInterface.Bar))!);
            }
        }

        [Benchmark]
        public void When_class_implements_interface_and_property_has_description_then_property_description_is_used()
        {
            for (int i = 0; i < N; i++)
            {
                // act
                s_documentationProvider.GetDescription(
                    typeof(ClassWithInterfaceAndCustomSummaries)
                        .GetProperty(nameof(ClassWithInterfaceAndCustomSummaries.Foo))!);
            }
        }

        [Benchmark]
        public void When_class_implements_interface_and_property_has_description_then_property_description_is_used_old()
        {
            for (int i = 0; i < N; i++)
            {
                // act
                s_oldDocumentationProvider.GetDescription(
                    typeof(ClassWithInterfaceAndCustomSummaries)
                        .GetProperty(nameof(ClassWithInterfaceAndCustomSummaries.Foo))!);
            }
        }

        [Benchmark]
        public void When_class_implements_interface_and_method_has_description_then_method_description_is_used()
        {
            for (int i = 0; i < N; i++)
            {
                // act
                s_documentationProvider.GetDescription(
                    typeof(ClassWithInterfaceAndCustomSummaries)
                        .GetMethod(nameof(ClassWithInterfaceAndCustomSummaries.Bar))!);
            }
        }

        [Benchmark]
        public void When_class_implements_interface_and_method_has_description_then_method_description_is_used_old()
        {
            for (int i = 0; i < N; i++)
            {
                // act
                s_oldDocumentationProvider.GetDescription(
                    typeof(ClassWithInterfaceAndCustomSummaries)
                        .GetMethod(nameof(ClassWithInterfaceAndCustomSummaries.Bar))!);
            }
        }

        [Benchmark]
        public void
            When_class_implements_interface_and_method_has_description_then_method_parameter_description_is_used()
        {
            for (int i = 0; i < N; i++)
            {
                // act
                s_documentationProvider.GetDescription(
                    typeof(ClassWithInterfaceAndCustomSummaries)
                        .GetMethod(nameof(ClassWithInterfaceAndCustomSummaries.Bar))!
                        .GetParameters()
                        .Single(p => p.Name == "baz"));
            }
        }

        [Benchmark]
        public void
            When_class_implements_interface_and_method_has_description_then_method_parameter_description_is_used_old()
        {
            for (int i = 0; i < N; i++)
            {
                // act
                s_oldDocumentationProvider.GetDescription(
                    typeof(ClassWithInterfaceAndCustomSummaries)
                        .GetMethod(nameof(ClassWithInterfaceAndCustomSummaries.Bar))!
                        .GetParameters()
                        .Single(p => p.Name == "baz"));
            }
        }

        [Benchmark]
        public void When_class_has_description_then_it_is_converted()
        {
            for (int i = 0; i < N; i++)
            {
                // act
                s_documentationProvider.GetDescription(
                    typeof(ClassWithSummary));
            }
        }

        [Benchmark]
        public void When_class_has_description_then_it_is_converted_old()
        {
            for (int i = 0; i < N; i++)
            {
                // act
                s_oldDocumentationProvider.GetDescription(
                    typeof(ClassWithSummary));
            }
        }

        [Benchmark]
        public void When_method_has_exceptions_then_it_is_converted()
        {
            for (int i = 0; i < N; i++)
            {
                // act
                s_documentationProvider.GetDescription(
                    typeof(WithExceptionsXmlDoc).GetMethod(nameof(WithExceptionsXmlDoc.Foo))!);
            }
        }

        [Benchmark]
        public void When_method_has_exceptions_then_it_is_converted_old()
        {
            for (int i = 0; i < N; i++)
            {
                // act
                s_oldDocumentationProvider.GetDescription(
                    typeof(WithExceptionsXmlDoc).GetMethod(nameof(WithExceptionsXmlDoc.Foo))!);
            }
        }

        [Benchmark]
        public void When_method_has_exceptions_then_exceptions_with_no_code_will_be_ignored()
        {
            for (int i = 0; i < N; i++)
            {
                // act
                s_documentationProvider.GetDescription(
                    typeof(WithExceptionsXmlDoc).GetMethod(nameof(WithExceptionsXmlDoc.Bar))!);
            }
        }

        [Benchmark]
        public void When_method_has_exceptions_then_exceptions_with_no_code_will_be_ignored_old()
        {
            for (int i = 0; i < N; i++)
            {
                // act
                s_oldDocumentationProvider.GetDescription(
                    typeof(WithExceptionsXmlDoc).GetMethod(nameof(WithExceptionsXmlDoc.Bar))!);
            }
        }

        [Benchmark]
        public void When_method_has_only_exceptions_with_no_code_then_error_section_will_not_be_written()
        {
            for (int i = 0; i < N; i++)
            {
                // act
                s_documentationProvider.GetDescription(
                    typeof(WithExceptionsXmlDoc).GetMethod(nameof(WithExceptionsXmlDoc.Baz))!);
            }
        }

        [Benchmark]
        public void When_method_has_only_exceptions_with_no_code_then_error_section_will_not_be_written_old()
        {
            for (int i = 0; i < N; i++)
            {
                // act
                s_oldDocumentationProvider.GetDescription(
                    typeof(WithExceptionsXmlDoc).GetMethod(nameof(WithExceptionsXmlDoc.Baz))!);
            }
        }

        [Benchmark]
        public void When_method_has_no_exceptions_then_it_is_ignored()
        {
            for (int i = 0; i < N; i++)
            {
                // act
                s_documentationProvider.GetDescription(
                    typeof(WithoutExceptionsXmlDoc).GetMethod(nameof(WithoutExceptionsXmlDoc.Foo))!);
            }
        }

        [Benchmark]
        public void When_method_has_no_exceptions_then_it_is_ignored_old()
        {
            for (int i = 0; i < N; i++)
            {
                // act
                s_oldDocumentationProvider.GetDescription(
                    typeof(WithoutExceptionsXmlDoc).GetMethod(nameof(WithoutExceptionsXmlDoc.Foo))!);
            }
        }

        [Benchmark]
        public void When_method_has_returns_then_it_is_converted()
        {
            for (int i = 0; i < N; i++)
            {
                // act
                s_documentationProvider.GetDescription(
                    typeof(WithReturnsXmlDoc).GetMethod(nameof(WithReturnsXmlDoc.Foo))!);
            }
        }


        [Benchmark]
        public void When_method_has_returns_then_it_is_converted_old()
        {
            for (int i = 0; i < N; i++)
            {
                // act
                s_oldDocumentationProvider.GetDescription(
                    typeof(WithReturnsXmlDoc).GetMethod(nameof(WithReturnsXmlDoc.Foo))!);
            }
        }

        [Benchmark]
        public void When_method_has_no_returns_then_it_is_ignored()
        {
            for (int i = 0; i < N; i++)
            {
                // act
                s_documentationProvider.GetDescription(
                    typeof(WithoutReturnsXmlDoc).GetMethod(nameof(WithoutReturnsXmlDoc.Foo))!);
            }
        }

        [Benchmark]
        public void When_method_has_no_returns_then_it_is_ignored_old()
        {
            for (int i = 0; i < N; i++)
            {
                // act
                s_oldDocumentationProvider.GetDescription(
                    typeof(WithoutReturnsXmlDoc).GetMethod(nameof(WithoutReturnsXmlDoc.Foo))!);
            }
        }

        [Benchmark]
        public void When_method_has_dictionary_args_then_it_is_found()
        {
            for (int i = 0; i < N; i++)
            {
                // act
                s_documentationProvider.GetDescription(
                    typeof(WithDictionaryArgs).GetMethod(nameof(WithDictionaryArgs.Method))!);
            }
        }

        [Benchmark]
        public void When_method_has_dictionary_args_then_it_is_found_old()
        {
            for (int i = 0; i < N; i++)
            {
                // act
                s_oldDocumentationProvider.GetDescription(
                    typeof(WithDictionaryArgs).GetMethod(nameof(WithDictionaryArgs.Method))!);
            }
        }
    }
}

/// <summary>
/// Resolves an XML documentation file from an assembly.
/// </summary>
public interface IOldXmlDocumentationFileResolver
{
    /// <summary>
    /// Trues to resolve an XML documentation file from the given assembly..
    /// </summary>
    bool TryGetXmlDocument(Assembly assembly,
        [NotNullWhen(true)] out XDocument? document);
}


public class OldXmlDocumentationFileResolver : IOldXmlDocumentationFileResolver
{
    private const string Bin = "bin";

    private readonly Func<Assembly, string>? _resolveXmlDocumentationFileName;

    private readonly ConcurrentDictionary<string, XDocument> _cache =
        new(StringComparer.OrdinalIgnoreCase);

    public OldXmlDocumentationFileResolver()
    {
        _resolveXmlDocumentationFileName = null;
    }

    public OldXmlDocumentationFileResolver(Func<Assembly, string>? resolveXmlDocumentationFileName)
    {
        _resolveXmlDocumentationFileName = resolveXmlDocumentationFileName;
    }

    public bool TryGetXmlDocument(
        Assembly assembly,
        [NotNullWhen(true)] out XDocument? document)
    {
        var fullName = assembly.GetName().FullName;

        if (!_cache.TryGetValue(fullName, out var doc))
        {
            var xmlDocumentFileName = GetXmlDocumentationPath(assembly);

            if (xmlDocumentFileName is not null && File.Exists(xmlDocumentFileName))
            {
                doc = XDocument.Load(xmlDocumentFileName, LoadOptions.PreserveWhitespace);
                _cache[fullName] = doc;
            }
        }

        document = doc;
        return document != null;
    }

    private string? GetXmlDocumentationPath(Assembly? assembly)
    {
        try
        {
            if (assembly is null)
            {
                return null;
            }

            var assemblyName = assembly.GetName();
            if (string.IsNullOrEmpty(assemblyName.Name))
            {
                return null;
            }

            if (_cache.ContainsKey(assemblyName.FullName))
            {
                return null;
            }

            var expectedDocFile = _resolveXmlDocumentationFileName is null
                ? $"{assemblyName.Name}.xml"
                : _resolveXmlDocumentationFileName(assembly);

            string path;
            if (!string.IsNullOrEmpty(assembly.Location))
            {
                var assemblyDirectory = IOPath.GetDirectoryName(assembly.Location);
                path = IOPath.Combine(assemblyDirectory!, expectedDocFile);
                if (File.Exists(path))
                {
                    return path;
                }
            }

#pragma warning disable SYSLIB0012
            var codeBase = assembly.CodeBase;
#pragma warning restore SYSLIB0012
            if (!string.IsNullOrEmpty(codeBase))
            {
                path = IOPath.Combine(
                    IOPath.GetDirectoryName(codeBase.Replace("file:///", string.Empty))!,
                    expectedDocFile)
                    .Replace("file:\\", string.Empty);

                if (File.Exists(path))
                {
                    return path;
                }
            }

            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            if (!string.IsNullOrEmpty(baseDirectory))
            {
                path = IOPath.Combine(baseDirectory, expectedDocFile);
                if (File.Exists(path))
                {
                    return path;
                }

                return IOPath.Combine(baseDirectory, Bin, expectedDocFile);
            }

            var currentDirectory = Directory.GetCurrentDirectory();
            path = IOPath.Combine(currentDirectory, expectedDocFile);
            if (File.Exists(path))
            {
                return path;
            }

            path = IOPath.Combine(currentDirectory, Bin, expectedDocFile);

            if (File.Exists(path))
            {
                return path;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}


public class OldXmlDocumentationProvider : IDocumentationProvider
{
    private const string SummaryElementName = "summary";
    private const string ExceptionElementName = "exception";
    private const string ReturnsElementName = "returns";
    private const string Inheritdoc = "inheritdoc";
    private const string See = "see";
    private const string Langword = "langword";
    private const string Cref = "cref";
    private const string Href = "href";
    private const string Code = "code";
    private const string Paramref = "paramref";
    private const string Name = "name";

    private readonly IOldXmlDocumentationFileResolver _fileResolver;
    private readonly ObjectPool<StringBuilder> _stringBuilderPool;

    public OldXmlDocumentationProvider(
        IOldXmlDocumentationFileResolver fileResolver,
        ObjectPool<StringBuilder> stringBuilderPool)
    {
        _fileResolver = fileResolver ?? throw new ArgumentNullException(nameof(fileResolver));
        _stringBuilderPool = stringBuilderPool;
    }

    public string? GetDescription(Type type) =>
        GetDescriptionInternal(type);

    public string? GetDescription(MemberInfo member) =>
        GetDescriptionInternal(member);

    public string? GetDescription(ParameterInfo parameter)
    {
        var element = GetParameterElement(parameter);

        if (element is null)
        {
            return null;
        }

        var description = new StringBuilder();
        AppendText(element, description);

        if (description.Length == 0)
        {
            return null;
        }

        return RemoveLineBreakWhiteSpaces(description.ToString());
    }

    private string? GetDescriptionInternal(MemberInfo member)
    {
        var element = GetMemberElement(member);

        if (element is null)
        {
            return null;
        }

        var description = ComposeMemberDescription(
            element.Element(SummaryElementName),
            element.Element(ReturnsElementName),
            element.Elements(ExceptionElementName));

        return RemoveLineBreakWhiteSpaces(description);
    }

    private string? ComposeMemberDescription(
        XElement? summary,
        XElement? returns,
        IEnumerable<XElement> errors)
    {
        var description = _stringBuilderPool.Get();

        try
        {
            var needsNewLine = false;

            if (!string.IsNullOrEmpty(summary?.Value))
            {
                AppendText(summary, description);
                needsNewLine = true;
            }

            if (!string.IsNullOrEmpty(returns?.Value))
            {
                AppendNewLineIfNeeded(description, needsNewLine);
                description.AppendLine("**Returns:**");
                AppendText(returns, description);
                needsNewLine = true;
            }

            AppendErrorDescription(errors, description, needsNewLine);

            return description.Length == 0 ? null : description.ToString();
        }
        finally
        {
            _stringBuilderPool.Return(description);
        }
    }

    private void AppendErrorDescription(
        IEnumerable<XElement> errors,
        StringBuilder description,
        bool needsNewLine)
    {
        var errorCount = 0;
        foreach (var error in errors)
        {
            var code = error.Attribute(Code);
            if (code is { }
                && !string.IsNullOrEmpty(error.Value)
                && !string.IsNullOrEmpty(code.Value))
            {
                if (errorCount == 0)
                {
                    AppendNewLineIfNeeded(description, needsNewLine);
                    description.AppendLine("**Errors:**");
                }
                else
                {
                    description.AppendLine();
                }

                description.Append($"{++errorCount}. ");
                description.Append($"{code.Value}: ");

                AppendText(error, description);
            }
        }
    }

    private static void AppendText(
        XElement? element,
        StringBuilder description)
    {
        if (element is null || string.IsNullOrWhiteSpace(element.Value))
        {
            return;
        }

        foreach (var node in element.Nodes())
        {
            if (node is not XElement currentElement)
            {
                if (node is XText text)
                {
                    description.Append(text.Value);
                }

                continue;
            }

            if (currentElement.Name == Paramref)
            {
                var nameAttribute = currentElement.Attribute(Name);

                if (nameAttribute != null)
                {
                    description.Append(nameAttribute.Value);
                    continue;
                }
            }

            if (currentElement.Name != See)
            {
                description.Append(currentElement.Value);
                continue;
            }

            var attribute = currentElement.Attribute(Langword);
            if (attribute != null)
            {
                description.Append(attribute.Value);
                continue;
            }

            if (!string.IsNullOrEmpty(currentElement.Value))
            {
                description.Append(currentElement.Value);
            }
            else
            {
                attribute = currentElement.Attribute(Cref);
                if (attribute != null)
                {
                    description.Append(attribute.Value
                        .Trim('!', ':').Trim()
                        .Split('.').Last());
                }
                else
                {
                    attribute = currentElement.Attribute(Href);
                    if (attribute != null)
                    {
                        description.Append(attribute.Value);
                    }
                }
            }
        }
    }

    private void AppendNewLineIfNeeded(
        StringBuilder description,
        bool condition)
    {
        if (condition)
        {
            description.AppendLine();
            description.AppendLine();
        }
    }

    private XElement? GetMemberElement(MemberInfo member)
    {
        try
        {
            if (_fileResolver.TryGetXmlDocument(
                member.Module.Assembly,
                out var document))
            {
                var name = GetMemberElementName(member);
                var element = document.XPathSelectElements(name.Path)
                    .FirstOrDefault();

                ReplaceInheritdocElements(member, element);

                return element;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private XElement? GetParameterElement(ParameterInfo parameter)
    {
        try
        {
            if (_fileResolver.TryGetXmlDocument(
                parameter.Member.Module.Assembly,
                out var document))
            {
                var name = GetMemberElementName(parameter.Member);
                var result = document.XPathSelectElements(name.Path);
                var element = result.FirstOrDefault();

                if (element is null)
                {
                    return null;
                }

                ReplaceInheritdocElements(parameter.Member, element);

                if (parameter.IsRetval
                    || string.IsNullOrEmpty(parameter.Name))
                {
                    result = document.XPathSelectElements(name.ReturnsPath);
                }
                else
                {
                    result = document.XPathSelectElements(
                        name.GetParameterPath(parameter.Name));
                }

                return result.FirstOrDefault();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private void ReplaceInheritdocElements(
        MemberInfo member,
        XElement? element)
    {
        if (element is null)
        {
            return;
        }

        var children = element.Nodes().ToList();
        foreach (var child in children.OfType<XElement>())
        {
            if (string.Equals(child.Name.LocalName, Inheritdoc,
                StringComparison.OrdinalIgnoreCase))
            {
                var baseType =
                    member.DeclaringType?.GetTypeInfo().BaseType;
                var baseMember =
                    baseType?.GetTypeInfo().DeclaredMembers
                        .SingleOrDefault(m => m.Name == member.Name);

                if (baseMember != null)
                {
                    var baseDoc = GetMemberElement(baseMember);
                    if (baseDoc != null)
                    {
                        var nodes =
                            baseDoc.Nodes().OfType<object>().ToArray();
                        child.ReplaceWith(nodes);
                    }
                    else
                    {
                        ProcessInheritdocInterfaceElements(member, child);
                    }
                }
                else
                {
                    ProcessInheritdocInterfaceElements(member, child);
                }
            }
        }
    }

    private void ProcessInheritdocInterfaceElements(
        MemberInfo member,
        XElement child)
    {
        if (member.DeclaringType is { })
        {
            foreach (var baseInterface in member.DeclaringType
                .GetTypeInfo().ImplementedInterfaces)
            {
                var baseMember = baseInterface.GetTypeInfo()
                    .DeclaredMembers.SingleOrDefault(m =>
                        m.Name.EqualsOrdinal(member.Name));
                if (baseMember != null)
                {
                    var baseDoc = GetMemberElement(baseMember);
                    if (baseDoc != null)
                    {
                        child.ReplaceWith(
                            baseDoc.Nodes().OfType<object>().ToArray());
                    }
                }
            }
        }
    }

    private static string? RemoveLineBreakWhiteSpaces(string? documentation)
    {
        if (string.IsNullOrWhiteSpace(documentation))
        {
            return null;
        }

        documentation =
            "\n" + documentation.Replace("\r", string.Empty).Trim('\n');

        var whitespace =
            Regex.Match(documentation, "(\\n[ \\t]*)").Value;

        documentation = documentation.Replace(whitespace, "\n");

        return documentation.Trim('\n').Trim();
    }

    private static MemberName GetMemberElementName(MemberInfo member)
    {
        char prefixCode;

        var memberName =
            member is Type { FullName: { Length: > 0 } } memberType
            ? memberType.FullName
            : member.DeclaringType is null
                ? member.Name
                : member.DeclaringType.FullName + "." + member.Name;

        switch (member.MemberType)
        {
            case MemberTypes.Constructor:
                memberName = memberName.Replace(".ctor", "#ctor");
                goto case MemberTypes.Method;

            case MemberTypes.Method:
                prefixCode = 'M';

                var paramTypesList = string.Join(",",
                    ((MethodBase)member).GetParameters()
                    .Select(x => Regex
                        .Replace(x.ParameterType.FullName!,
                            "(`[0-9]+)|(, .*?PublicKeyToken=[0-9a-z]*)",
                            string.Empty)
                        .Replace("[[", "{")
                        .Replace("]]", "}")
                        .Replace("],[", ","))
                    .ToArray());

                if (!string.IsNullOrEmpty(paramTypesList))
                {
                    memberName += "(" + paramTypesList + ")";
                }

                break;

            case MemberTypes.Event:
                prefixCode = 'E';
                break;

            case MemberTypes.Field:
                prefixCode = 'F';
                break;

            case MemberTypes.NestedType:
                memberName = memberName?.Replace('+', '.');
                goto case MemberTypes.TypeInfo;

            case MemberTypes.TypeInfo:
                prefixCode = 'T';
                break;

            case MemberTypes.Property:
                prefixCode = 'P';
                break;

            default:
                throw new ArgumentException(
                    "Unknown member type.",
                    nameof(member));
        }

        return new MemberName(
            $"{prefixCode}:{memberName?.Replace("+", ".")}");
    }

    private ref struct MemberName
    {
        private const string GetMemberDocPathFormat = "/doc/members/member[@name='{0}']";
        private const string ReturnsPathFormat = "{0}/returns";
        private const string ParamsPathFormat = "{0}/param[@name='{1}']";

        public MemberName(string name)
        {
            Value = name;
            Path = string.Format(
                CultureInfo.InvariantCulture,
                GetMemberDocPathFormat,
                name);
            ReturnsPath = string.Format(
                CultureInfo.InvariantCulture,
                ReturnsPathFormat,
                Path);
        }

        public string Value { get; }

        public string Path { get; }

        public string ReturnsPath { get; }

        public string GetParameterPath(string name)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                ParamsPathFormat,
                Path,
                name);
        }
    }
}


