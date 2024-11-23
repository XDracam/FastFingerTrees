namespace DracTec.FTrees.Impl;

internal readonly struct View<T, V>(T head, FTree<T, V> tail)
    where T : IFTreeElement<V>
    where V : struct, IFTreeMeasure<V>
{
    public View() : this(default, default) { }
    public readonly bool IsCons = true;
    public readonly T Head = head;
    public readonly FTree<T, V> Tail = tail;
}