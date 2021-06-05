using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using vinkekfish;
using keccak;   // keccak взят отсюда https://github.com/fdsc/old/releases (это мой старый проект)
using BytesBuilder = cryptoprime.BytesBuilder;
using System.Runtime.CompilerServices;

namespace main_tests
{
    class VinKekFishSimpleHashTest
    {
        readonly TestTask task;
        public VinKekFishSimpleHashTest(ConcurrentQueue<TestTask> tasks)
        {
            task = new TestTask("VinKekFish simple hash test VinKekFishBase_KN_20210525 (VinKekFishSimpleHashTest)", StartTests);
            tasks.Enqueue(task);

            sources = SourceTask.getIterator();
        }

        class SourceTask
        {
            public string Key;
            public byte[] Value;

            public int    K = 1;

            public static IEnumerable<SourceTask> getIterator()
            {
                yield return new SourceTask() {Key = "byte[0]", Value = new byte[0]};
                yield return new SourceTask() {Key = "Свет мой зеркальце скажи", Value = new UTF8Encoding().GetBytes("Свет мой зеркальце скажи")};

                const int step = 255;
                const int Size = 1; // 1024
                for (int K = 1; K <= 19; K += 2)
                for (int size = 1; size <= Size; size++)
                {
                    for (int val = 0; val <= 255; val += step) // += 7 - это просто чтобы меньше было задач
                    {
                        var b1 = new byte[size];
                        var b2 = new byte[size];
                        BytesBuilder.ToNull(b1);
                        BytesBuilder.FillByBytes(  (byte) val, b2  );
                        b1[0] = (byte) val;

                        yield return new SourceTask() {Key = "byte[" + size + "] with nulls and val = " + val, Value = b1, K = K};
                        yield return new SourceTask() {Key = "byte[" + size + "] with vals = " + val, Value = b2, K = K};
                    }
                }

                yield break;
            }
        }

        readonly IEnumerable<SourceTask> sources = null;

        public unsafe void StartTests()
        {
            foreach (var ts in sources)
            {
                var s = BytesBuilder.CloneBytes(ts.Value);

                var k = new VinKekFishBase_KN_20210525();
                //byte[] h1, h2;
                fixed (byte * Sb = s)
                {
                    k.Init1();
                }
                k.Dispose();
                /*
                if (!BytesBuilder.UnsecureCompare(s, ts.Value))
                {
                    task.error.Add(new Error() {Message = "Sources arrays has been changed for test array: " + ts.Key});
                }

                if (!BytesBuilder.UnsecureCompare(h1, h2))
                {
                    task.error.Add(new Error() {Message = "Hashes are not equal for test array: " + ts.Key});
                }*/
            }
        }
    }
}
