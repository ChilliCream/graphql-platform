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
            Test?[]?[]? objectNested,
            bool detailedOutput = false)
        {
            if (single is not null)
            {
                return Format(single, detailedOutput)!;
            }

            if (list is not null)
            {
                return string.Join(",", list.Select(x => Format(x, detailedOutput) ?? "null"));
            }

            if (nested is not null)
            {
                return string.Join(",",
                    nested.SelectMany(y => y!.Select(x => Format(x, detailedOutput) ?? "null")));
            }

            if (objectSingle is not null)
            {
                return Format(objectSingle.Bar!.Baz!.File, detailedOutput)!;
            }

            if (objectList is not null)
            {
                return string.Join(",",
                    objectList.Select(x => Format(x?.Bar!.Baz!.File, detailedOutput) ?? "null"));
            }

            if (objectNested is not null)
            {
                return string.Join(",",
                    objectNested.SelectMany(y
                        => y!.Select(x => Format(x?.Bar!.Baz!.File, detailedOutput) ?? "null")));
            }

            return "error";
        }

        private static string? Format(IFile? file, bool detailed) =>
            detailed ? $"[{file?.ReadContents()}|{file?.Name}|{file?.ContentType}]" : file?.ReadContents();
    }

    public record Test(Bar? Bar);

    public record Bar(Baz? Baz);

    public record Baz(IFile? File);
}
