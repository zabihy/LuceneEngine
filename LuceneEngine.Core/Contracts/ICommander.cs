namespace LuceneEngine.Core
{
    public interface ICommander<TOutput>
    {
        TOutput Next();
    }
}