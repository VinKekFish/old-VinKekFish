using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using vinkekfish;
using keccak;   // keccak взят отсюда https://github.com/fdsc/old/releases
using BytesBuilder = cryptoprime.BytesBuilder;
using System.Runtime.CompilerServices;

namespace main_tests
{
    class KeccakSingleHashPerformanceTest
    {
        readonly TestTask task;
        public KeccakSingleHashPerformanceTest(ConcurrentQueue<TestTask> tasks)
        {
            task = new TestTask("Keccak Single Hash Performance Test Keccak_base_20200918.getHash512 (Keccak_20200918)", StartTests);
            tasks.Enqueue(task);
            task.waitAfter  = true;
            task.waitBefore = true;
        }

        public TimeSpan timeForMillion;

        public unsafe void StartTests()
        {
            var s = new UTF8Encoding().GetBytes("Свет мой зеркальце скажи");

            var k = new Keccak_20200918();
            byte[] h1;
            var dt1 = DateTime.Now;

            const int times = 1000_000;
            for (int i = 0; i < times; i++)
            fixed (byte * Sb = s)
            {
                h1 = k.getHash512(Sb, s.LongLength, true);
                // h2 = new SHA3(1024).getHash512(s);
            }

            var dt2 = DateTime.Now;
            timeForMillion = dt2-dt1;
            var CountsPermsecond = times / timeForMillion.TotalMilliseconds;

            // Должно быть порядка 330 тысяч в секунду на одном старом ядре 2,8 ГГц с оптимизацией в cryptoprime, но без оптимизаций в остальных проектах
            if (CountsPermsecond < 270)
                task.error.Add(new Error() {Message = "Slow execution for hash512: count " + CountsPermsecond +  " pre 1 ms time " + HelperClass.TimeStampTo_HHMMSSfff_String(timeForMillion)});
        }
    }
}
