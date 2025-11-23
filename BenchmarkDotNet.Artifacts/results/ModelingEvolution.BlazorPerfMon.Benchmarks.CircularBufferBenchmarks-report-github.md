```

BenchmarkDotNet v0.15.6, Linux Ubuntu 24.04.2 LTS (Noble Numbat)
Intel Core i9-9980HK CPU 2.40GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v3

IterationCount=10  WarmupCount=3  

```
| Method                   | N   | Mean        | Error       | StdDev      | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------- |---- |------------:|------------:|------------:|------:|--------:|-------:|-------:|----------:|------------:|
| **AddSample_ImmutableQueue** | **100** |  **4,356.6 ns** |   **585.60 ns** |   **348.48 ns** |  **1.01** |    **0.10** | **1.1749** | **0.0381** |    **9880 B** |        **1.00** |
| AddSample_ImmutableArray | 100 |  2,872.9 ns |   170.17 ns |   112.55 ns |  0.66 |    0.05 | 1.7471 | 0.0458 |   14624 B |        1.48 |
| AddSample_ImmutableList  | 100 |  1,013.3 ns |   122.10 ns |    80.76 ns |  0.23 |    0.02 | 0.2041 |      - |    1720 B |        0.17 |
| Zip_ImmutableQueue       | 100 |  6,843.1 ns |   920.34 ns |   608.75 ns |  1.58 |    0.17 | 0.0305 |      - |     336 B |        0.03 |
| Zip_ImmutableArray       | 100 |    516.1 ns |    58.29 ns |    38.56 ns |  0.12 |    0.01 |      - |      - |         - |        0.00 |
| Zip_ImmutableList        | 100 |  2,076.8 ns |   374.33 ns |   247.60 ns |  0.48 |    0.06 |      - |      - |         - |        0.00 |
|                          |     |             |             |             |       |         |        |        |           |             |
| **AddSample_ImmutableQueue** | **200** |  **8,884.0 ns** |   **949.15 ns** |   **627.80 ns** |  **1.00** |    **0.09** | **2.3193** | **0.1526** |   **19480 B** |        **1.00** |
| AddSample_ImmutableArray | 200 |  5,976.7 ns |   549.95 ns |   363.76 ns |  0.68 |    0.06 | 3.4637 | 0.1755 |   29024 B |        1.49 |
| AddSample_ImmutableList  | 200 |  1,028.9 ns |    91.37 ns |    60.44 ns |  0.12 |    0.01 | 0.2308 |      - |    1944 B |        0.10 |
| Zip_ImmutableQueue       | 200 | 13,116.9 ns | 1,984.78 ns | 1,181.11 ns |  1.48 |    0.16 | 0.0305 |      - |     336 B |        0.02 |
| Zip_ImmutableArray       | 200 |  1,013.5 ns |   120.14 ns |    79.47 ns |  0.11 |    0.01 |      - |      - |         - |        0.00 |
| Zip_ImmutableList        | 200 |  4,512.7 ns |   476.81 ns |   249.38 ns |  0.51 |    0.04 |      - |      - |         - |        0.00 |
|                          |     |             |             |             |       |         |        |        |           |             |
| **AddSample_ImmutableQueue** | **500** | **18,515.9 ns** | **1,173.23 ns** |   **698.17 ns** |  **1.00** |    **0.05** | **5.7678** | **0.8545** |   **48280 B** |       **1.000** |
| AddSample_ImmutableArray | 500 | 12,921.3 ns |   951.20 ns |   497.50 ns |  0.70 |    0.04 | 8.5754 | 1.0681 |   72224 B |       1.496 |
| AddSample_ImmutableList  | 500 |  1,182.7 ns |   254.92 ns |   151.70 ns |  0.06 |    0.01 | 0.2575 |      - |    2168 B |       0.045 |
| Zip_ImmutableQueue       | 500 | 32,010.1 ns | 1,542.24 ns |   806.62 ns |  1.73 |    0.07 | 0.0305 |      - |     336 B |       0.007 |
| Zip_ImmutableArray       | 500 |  2,450.7 ns |   247.82 ns |   147.47 ns |  0.13 |    0.01 |      - |      - |         - |       0.000 |
| Zip_ImmutableList        | 500 | 41,628.6 ns | 4,079.63 ns | 2,698.42 ns |  2.25 |    0.16 |      - |      - |         - |       0.000 |
