using StrawberryShake.CodeGeneration.CSharp;

namespace StrawberryShake.Tools;

public static class GeneratorResponseExtensions
{
    public static bool TryLogErrors(this GeneratorResponse response, IActivity activity)
    {
        if (response.Errors.Count > 0)
        {
            foreach (GeneratorError error in response.Errors)
            {
                Dictionary<string, object?>? extensions = null;
                if (error.FilePath is not null)
                {
                    (extensions ??= new())["filePath"] = error.FilePath;
                }

                List<HotChocolate.Location>? locations = null;
                if (error.Location is not null)
                {
                    (locations ??= new()).Add(
                        new HotChocolate.Location(
                            error.Location.Line,
                            error.Location.Column));
                }

                activity.WriteError(new HotChocolate.Error(
                    error.Message,
                    error.Code,
                    locations: locations,
                    extensions: extensions));
            }

            return true;
        }

        return false;
    }
}
