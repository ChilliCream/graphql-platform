namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public interface ICodeContainer<T>: ICode
    {
        public T AddCode(string value);
        public T AddCode(ICode code, bool addIf = true);
        public T AddEmptyLine();
    }
}
