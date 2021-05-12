using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using vinkekfish;

namespace main_tests
{
    class KeccakStatesArray_CountToCheck_test
    {
        TestTask task;
        public KeccakStatesArray_CountToCheck_test(ConcurrentQueue<TestTask> tasks)
        {
            task = new TestTask("KeccakStatesArray_CountToCheck_test", StartTests);
            task.waitAfter  = true;
            task.waitBefore = true;
            tasks.Enqueue(task);
        }

        public void StartTests()
        {
            task.error.Add(new Error(){ Message = "vinkekfish.Keccak_abstract.KeccakStatesArray.getCountToCheck != 0: TEST NOT IMPLEMENTED!" });
        }
    }
}
