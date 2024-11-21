# Fast Finger Trees

A brutally micro-optimized implementation of 2-3 finger trees as introduced in [this paper](https://www.staff.city.ac.uk/~ross/papers/FingerTree.pdf) (DOI `10.1017/S0956796805005769`).
This implementation is competitve with other immutable collections from the `System.Collections.Immutable` namespace, even massively exceeding their performance in some scenarios.

> [!NOTE]
> This is a hobby project of mine. I want to see how far I can push this.
> As a consequence, unit tests are AI generated and the benchmarks are written for convenience over professionality.
> There is no reason why I am doing this and I have no plans to ever use this in production.
> Do not take this project too seriously (yet!)

Finger trees are an abstract "backing collection" that can be used as a base to implement a diverse set of more specific collections. 
This repository a number of demo collections all based on the same `FTree<T,V>` implementation:

1. `ImmutableSeq<T>` (an `IImmutableList<T>` implementation)
2. `ImmutableOrderedSet<T>` (not an `IImmutableSet<T>` implementation, but supports fast `Contains`, `Add`, `Remove`, `Union` and `Partition`)
3. `ImmutableMaxPriorityQueue<T>` (a simple priority queue supporting `Enqueue` and `ExtractMax`)
4. `ImmutableIntervalTree<T, P>` (a tree of intervals with bounds of type `P` and keys `T` which allows fast queries for intersecting intervals)

From these, `ImmutableSeq<T>` is the most optimized. It is a drop-in replacement for `ImmutableList<T>` that is significantly faster when modified close to the start or end and slower when modified in the middle.
A specialty of this sequence is concatenation, which runs in **amortized O(1) constant time**, which is especially impressive when considering that most collections concatenate in linear time.

Don't believe me? Look at the following Benchmark.NET results for concatenating a collection of the specified size to itself via `AddRange`:


| Method               | Count | Mean          | Error      | StdDev     |
|--------------------- |------ |--------------:|-----------:|-----------:|
| **ListConcat**           | **100**   |      **55.94 ns** |   **0.585 ns** |   **0.518 ns** |
| ImmutableArrayConcat | 100   |      34.48 ns |   0.572 ns |   0.535 ns |
| ImmutableListConcat  | 100   |     212.92 ns |   1.378 ns |   1.151 ns |
| ImmutableSeqConcat   | 100   |      37.68 ns |   0.166 ns |   0.130 ns |
| **ListConcat**           | **1000**  |     **339.37 ns** |   **6.137 ns** |   **5.741 ns** |
| ImmutableArrayConcat | 1000  |     318.13 ns |   4.121 ns |   3.654 ns |
| ImmutableListConcat  | 1000  |     383.31 ns |   1.571 ns |   1.392 ns |
| ImmutableSeqConcat   | 1000  |      36.49 ns |   0.275 ns |   0.244 ns |
| **ListConcat**           | **10000** |   **3,783.06 ns** |  **18.999 ns** |  **17.772 ns** |
| ImmutableArrayConcat | 10000 |   3,589.72 ns |   6.883 ns |   5.748 ns |
| ImmutableListConcat  | 10000 |     670.83 ns |   1.656 ns |   1.549 ns |
| ImmutableSeqConcat   | 10000 |      36.35 ns |   0.233 ns |   0.218 ns |
| **ListConcat**           | **30000** | **106,692.13 ns** | **575.972 ns** | **510.584 ns** |
| ImmutableArrayConcat | 30000 | 104,429.78 ns | 407.362 ns | 318.041 ns |
| ImmutableListConcat  | 30000 |     701.11 ns |   2.050 ns |   1.817 ns |
| ImmutableSeqConcat   | 30000 |      36.93 ns |   0.690 ns |   0.612 ns |

As long as both collections are `ImmutableSeq<T>`s, we get instant concatenation! 
Concatenating an array is also an order of magnitude faster than `ImmutableList<T>`, and simply appending and prepending is 4x as fast. 
Inserting in the middle is 4x slower due to how finger trees work. Enumeration and indexing performance are roughly equivalent.
