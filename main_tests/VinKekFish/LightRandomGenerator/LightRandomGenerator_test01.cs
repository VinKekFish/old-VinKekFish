using cryptoprime.VinKekFish;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using vinkekfish;

namespace main_tests
{
    unsafe class LightRandomGenerator_test01
    {
        TestTask task;
        public LightRandomGenerator_test01(ConcurrentQueue<TestTask> tasks)
        {
            task = new TestTask("LightRandomGenerator_test01", StartTests);
            tasks.Enqueue(task);
        }

        public void StartTests()
        {
            using var gen = new VinKekFish_k1_base_20210419_keyGeneration();
            gen.Init1(VinKekFishBase_etalonK1.NORMAL_ROUNDS, null, 0);

            var key = new byte[] {1};

            fixed (byte * k = key)
                gen.Init2(k, 1, null);

            gen.EnterToBackgroundCycle(72);
            Thread.Sleep(10_000);
            gen.ExitFromBackgroundCycle();
            task.error.Add(new Error() { Message = $"LightRandomGenerator_test01. Generated bits: {gen.BackgourndGenerated}" });
        }
    }
}
