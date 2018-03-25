namespace Prometheus.Language
{
    public interface ISource
    {
        char Read(int position);
        string Read(int startIndex, int length);
        bool IsEndOfStream(int position);
    }
}