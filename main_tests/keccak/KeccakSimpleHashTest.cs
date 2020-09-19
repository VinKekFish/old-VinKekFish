using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using vinkekfish;
using keccak;

namespace main_tests
{
    class KeccakSimpleHashTest
    {
        TestTask task;
        public KeccakSimpleHashTest(ConcurrentQueue<TestTask> tasks)
        {
            task = new TestTask("Keccak simple hash test Keccak_base_20200918.getHash512 (Keccak_20200918)", StartTests);
            tasks.Enqueue(task);

            sources.Add("byte[0]",
                        new byte[0]);

            sources.Add("Свет мой зеркальце скажи",
                        new UTF8Encoding().GetBytes("Свет мой зеркальце скажи"));

            for (int size = 0; size < 1024; size++)
            {
                for (int val = 0; val <= 255; val++)
                {
                    
                }
            }
        }

        SortedList<string, byte[]> sources = new SortedList<string, byte[]>(128);

        public void StartTests()
        {
            foreach (var ts in sources)
            {
                var s = vinkekfish.BytesBuilder.CloneBytes(ts.Value);

                var k = new Keccak_20200918();
                var h1 = k.getHash512(s);
                var h2 = new SHA3(1024).getHash512(s);

                if (!vinkekfish.BytesBuilder.UnsecureCompare(s, ts.Value))
                {
                    task.error.Add(new Error() {Message = "Sources arrays has been changed for test array: " + ts.Key});
                }

                if (!vinkekfish.BytesBuilder.UnsecureCompare(h1, h2))
                {
                    task.error.Add(new Error() {Message = "Hashes are not equal for test array: " + ts.Key});
                }
            }
        }
    }
}
