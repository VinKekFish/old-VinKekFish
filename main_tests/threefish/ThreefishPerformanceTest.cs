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
using alien_SkeinFish;
using cryptoprime;
using CodeGenerated.Cryptoprimes;

namespace main_tests
{
    class ThreefishPerformanceTest
    {
        readonly TestTask task;
        public   double   CountsPermsecond_alienThreeFish  = 0;
        public   double   CountsPermsecond_slowlyThreeFish = 0;
        public   double   CountsPermsecond_GenThreeFish     = 0;
        public ThreefishPerformanceTest(ConcurrentQueue<TestTask> tasks)
        {
            task = new TestTask("ThreefishPerformanceTest", StartTests)
            {
                waitAfter  = true,
                waitBefore = true
            };
            tasks?.Enqueue(task);
        }

        public TimeSpan timeForMillion;

        public unsafe void StartTests()
        {
            TestForAlien();
            // TestForSlowly();
            TestForGen();
        }

        private unsafe void TestForAlien()
        {
            var bn = new byte[128];

            byte[] h1 = new byte[128];
            var tft = new ThreefishTransform(new byte[128], bn, ThreefishTransformMode.Encrypt);
            var tw = new ulong[2];
            tft.SetTweak(tw);
            h1 = new byte[128];

            var dt1 = DateTime.Now;

            const int times = 1000_000;
            for (int i = 0; i < times; i++)
            {
                tft.TransformBlock(h1, 0, 128, h1, 0);
            }

            var dt2 = DateTime.Now;
            timeForMillion = dt2 - dt1;
            CountsPermsecond_alienThreeFish = times / timeForMillion.TotalMilliseconds;

            // Должно быть порядка 330 тысяч в секунду на одном старом ядре 2,8 ГГц с оптимизацией в cryptoprime, но без оптимизаций в остальных проектах
            //if (CountsPermsecond_alienThreeFish < 270)
                task.error.Add(new Error() {Message = "CountsPermsecond_alienThreeFish: count " + CountsPermsecond_alienThreeFish +  " pre 1 ms time " + HelperClass.TimeStampTo_HHMMSSfff_String(timeForMillion)});
        }

        private unsafe void TestForGen()
        {
            var tft = new cryptoprime.Threefish1024(new byte[128], new byte[16]);
            var h1 = new byte[128];

            var dt1 = DateTime.Now;

            const int times = 1000_000;
            for (int i = 0; i < times; i++)
            {
                fixed (ulong * key = tft.key, tweak = tft.tweak)
                fixed (byte * h = h1)
                {
                    var hu = (ulong *) h;
                    Threefish_Static_Generated.Threefish1024_step(key, tweak, hu);
                }
            }

            var dt2 = DateTime.Now;
            timeForMillion = dt2 - dt1;
            CountsPermsecond_GenThreeFish = times / timeForMillion.TotalMilliseconds;

            // Должно быть порядка 330 тысяч в секунду на одном старом ядре 2,8 ГГц с оптимизацией в cryptoprime, но без оптимизаций в остальных проектах
            //if (CountsPermsecond_alienThreeFish < 270)
                task.error.Add(new Error() {Message = "CountsPermsecond_GenThreeFish: count " + CountsPermsecond_GenThreeFish +  " pre 1 ms time " + HelperClass.TimeStampTo_HHMMSSfff_String(timeForMillion)});
        }

        private unsafe void TestForSlowly()
        {
            var tw = new ulong[2];
            var k1 = new ulong[16];
            var k2 = new ulong[16];

            var dt1 = DateTime.Now;

            const int times = 1000_000;
            for (int i = 0; i < times; i++)
            {
                threefish_slowly.UlongToBytes(threefish_slowly.Encrypt(k1, tw, k2), null);
            }

            var dt2 = DateTime.Now;
            timeForMillion = dt2 - dt1;
            CountsPermsecond_slowlyThreeFish = times / timeForMillion.TotalMilliseconds;

            // Должно быть порядка 330 тысяч в секунду на одном старом ядре 2,8 ГГц с оптимизацией в cryptoprime, но без оптимизаций в остальных проектах
            // if (CountsPermsecond < 270)
                task.error.Add(new Error() {Message = "CountsPermsecond_slowlyThreeFish: count " + CountsPermsecond_slowlyThreeFish +  " pre 1 ms time " + HelperClass.TimeStampTo_HHMMSSfff_String(timeForMillion)});
        }
    }
}
