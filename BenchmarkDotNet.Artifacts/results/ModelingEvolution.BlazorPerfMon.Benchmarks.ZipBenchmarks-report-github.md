```

BenchmarkDotNet v0.15.6, Linux Ubuntu 24.04.2 LTS (Noble Numbat)
Intel Core i9-9980HK CPU 2.40GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v3

IterationCount=10  WarmupCount=3  

```
| Method                               | N   | Mean         | Error         | StdDev       | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------- |---- |-------------:|--------------:|-------------:|------:|--------:|-------:|----------:|------------:|
| **LinqZip_IEnumerable**                  | **10**  |    **130.69 ns** |      **4.984 ns** |     **2.966 ns** |  **1.00** |    **0.03** | **0.0191** |     **160 B** |        **1.00** |
| ZipValues_IEnumerable                | 10  |     79.91 ns |      3.472 ns |     2.297 ns |  0.61 |    0.02 | 0.0095 |      80 B |        0.50 |
| ZipValues_SampleAccessor             | 10  |    579.35 ns |     24.393 ns |    12.758 ns |  4.44 |    0.13 | 0.0401 |     336 B |        2.10 |
| ZipValues_SampleAccessor_IEnumerable | 10  |    354.07 ns |     25.568 ns |    16.912 ns |  2.71 |    0.14 | 0.0248 |     208 B |        1.30 |
|                                      |     |              |               |              |       |         |        |           |             |
| **LinqZip_IEnumerable**                  | **100** |    **876.72 ns** |    **139.565 ns** |    **92.314 ns** |  **1.01** |    **0.15** | **0.0191** |     **160 B** |        **1.00** |
| ZipValues_IEnumerable                | 100 |    676.34 ns |     69.208 ns |    36.197 ns |  0.78 |    0.09 | 0.0095 |      80 B |        0.50 |
| ZipValues_SampleAccessor             | 100 |  6,435.40 ns |    491.985 ns |   257.318 ns |  7.42 |    0.83 | 0.0381 |     336 B |        2.10 |
| ZipValues_SampleAccessor_IEnumerable | 100 |  3,267.16 ns |    474.809 ns |   314.057 ns |  3.77 |    0.53 | 0.0229 |     208 B |        1.30 |
|                                      |     |              |               |              |       |         |        |           |             |
| **LinqZip_IEnumerable**                  | **500** |  **4,554.73 ns** |    **702.472 ns** |   **418.030 ns** |  **1.01** |    **0.12** |      **-** |     **160 B** |        **1.00** |
| ZipValues_IEnumerable                | 500 |  3,095.26 ns |    307.319 ns |   160.734 ns |  0.68 |    0.06 | 0.0076 |      80 B |        0.50 |
| ZipValues_SampleAccessor             | 500 | 44,944.05 ns | 10,472.339 ns | 6,926.804 ns |  9.94 |    1.66 |      - |     336 B |        2.10 |
| ZipValues_SampleAccessor_IEnumerable | 500 | 16,432.76 ns |  2,009.899 ns | 1,329.424 ns |  3.63 |    0.40 |      - |     208 B |        1.30 |
