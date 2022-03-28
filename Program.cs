using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using ConnectBooster.Identity.Data.Utilities;
using slugger;

//BenchmarkRunner.Run<Md5VsSha256>();

//BenchmarkRunner.Run<NullCoalescingOverIf>();

BenchmarkRunner.Run<PriorityQB>();

namespace slugger
{
    [MemoryDiagnoser]
    public class Md5VsSha256
    {
        private const int N = 10000;
        private readonly byte[] data;

        private readonly SHA256 sha256 = SHA256.Create();
        private readonly MD5 md5 = MD5.Create();

        public Md5VsSha256()
        {
            data = new byte[N];
            new Random(42).NextBytes(data);
        }

        [Benchmark]
        public byte[] Sha256()
        {
            return sha256.ComputeHash(data);
        }

        [Benchmark]
        public byte[] Md5()
        {
            return md5.ComputeHash(data);
        }
    }

    [MemoryDiagnoser]
    public class NullCoalescingOverIf
    {
        //|        Method |      Mean |     Error |    StdDev |
        //|-------------- |----------:|----------:|----------:|
        //| NullCoalescer | 0.4996 ns | 0.0079 ns | 0.0070 ns |
        //|          Ifer | 0.2863 ns | 0.0213 ns | 0.0200 ns |

        //private readonly IEnumerable<object>? list = Enumerable.Empty<object>();
        private readonly IEnumerable<object>? list = null;

        public NullCoalescingOverIf() { }

        [Benchmark]
        public void NullCoalescer()
        {
            if (!list?.Any() ?? true)
            {
                return;
            }
        }

        // Winner
        [Benchmark]
        public void Ifer()
        {
            if (list == null || !list.Any())
            {
                return;
            }
        }
    }

    [MemoryDiagnoser]
    public class PriorityQB
    {
        private const int N = 1000000;
        private readonly UserEventLogger listH = new();
        private readonly UserEventLogger listL = new();

        public PriorityQB()
        {
            // for N
            for (var i = 0; i < N; ++i)
            {
                var type = new Random(42).Next(0, 10);
                var random = new Random(42).Next();
                listL.EnqueueEvent(new UserEventLog(Guid.NewGuid(), Guid.NewGuid(), type, "", random, DateTime.Now));
                listH.EnqueueEvent(new UserEventLog(Guid.NewGuid(), Guid.NewGuid(), type, "", random, DateTime.Now));
            }
        }

        [Benchmark]
        public void PriorityL()
        {
            listL.ProcessQueue_L();
        }

        [Benchmark]
        public void PriorityH()
        {
            listH.ProcessQueue_H();
        }
    }
}
