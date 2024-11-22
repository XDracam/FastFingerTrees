namespace DracTec.FTrees.Impl;

internal readonly struct View<T, V>(T head, LazyThunk<FTree<T, V>> tail)
    where T : IFTreeElement<V>
    where V : struct, IFTreeMeasure<V>
{
    public View() : this(default, default) { }
    public readonly bool IsCons = true;
    public readonly T Head = head;
    public FTree<T, V> Tail => tail.Value;
}