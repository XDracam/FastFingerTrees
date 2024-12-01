using System.Runtime.CompilerServices;

namespace DracTec.FTrees.Benchmarks.ImmutableSeq;

public static class _Utils
{
    public const int ParamsSize = 5000;
    
    // force value to be used
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Use<T>(T value) { }
}