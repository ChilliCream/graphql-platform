using System.Text;
using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace StrawberryShake.CodeGeneration.CSharp.Integration.UploadScalar;

public static class UploadSchemaHelpers
{
    public static MemoryStream CreateStream(string str)
    {
        var data = new MemoryStream();
        data.Write(Encoding.UTF8.GetBytes(str));
        data.Position = 0;
        return data;
    }

    public static void Configure(IRequestExecutorBuilder builder)
    {
        builder.AddTypeExtension<UploadQueries>().AddUploadType();
    }

    public static string ReadContents(this IFile file)
    {
        using var stream = file.OpenReadStream();
        using var read = new StreamReader(stream);
        return file.Name + ":" + read.ReadToEnd();
    }

    [ExtendObjectType("Query")]
    public class UploadQueries
    {
        public string Upload(
            string? nonUpload,
            IFile? single,
            IFile?[]? list,
            IFile?[]?[]? nested,
            [GraphQLName("object")] Test? objectSingle,
            Test?[]? objectList,
            Test?[]?[]? objectNested)
        {
            if (single is not null)
            {
                return Format(single);
            }

            if (list is not null)
            {
                return string.Join(",", list.Select(x => Format(x)));
            }

            if (nested is not null)
            {
                return string.Join(",",
                    nested.SelectMany(y => y!.Select(x => Format(x))));
            }

            if (objectSingle is not null)
            {
                return Format(objectSingle.Bar!.Baz!.File);
            }

            if (objectList is not null)
            {
                return string.Join(",",
                    objectList.Select(x => Format(x?.Bar!.Baz!.File) ?? "null"));
            }

            if (objectNested is not null)
            {
                return string.Join(",",
                    objectNested.SelectMany(y
                        => y!.Select(x => Format(x?.Bar!.Baz!.File) ?? "null")));
            }

            return "error";
        }

        private static string Format(IFile? file) =>
            $"[{file?.ReadContents()}|{file?.ContentType}]";
    }

    public record Test(Bar? Bar);

    public record Bar(Baz? Baz);

    public record Baz(IFile? File);
}
