namespace StrawberryShake.CodeGeneration.CSharp.Extensions
{
    public static class BoolExtensions
    {
        public static string IfTruePrint(this bool condition, string textToPrint)
        {
            return condition ? textToPrint : "";
        }
    }
}
