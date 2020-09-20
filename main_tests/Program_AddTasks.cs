using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace main_tests
{
    public class Error
    {
        public Exception ex = null;
        public string    Message = "";
    }

    public class TestTask
    {

        public TestTask(string Name, TestTaskFn task)
        {
            this.Name = Name;
            this.task = task;
        }

        public readonly TestTaskFn  task;
        public readonly string      Name;
        public          bool        start = false;
        public          bool        ended = false;
        public readonly List<Error> error = new List<Error>();

        public DateTime started = default(DateTime);
        public DateTime endTime = default(DateTime);

        public bool waitBefore = false;
        public bool waitAfter  = false;
    }

    public delegate void TestTaskFn();

    partial class Program
    {
        private static void AddTasks(ConcurrentQueue<TestTask> tasks)
        {
            ulong a = 0, b = 0;
            byte r = 0;
            cryptoprime.threefish.Mix(ref a, ref b, r);

            // Это делаем однопоточно, чтобы точно не помешать другим потомкам, т.к. это, по сути, аварийное выделение памяти
            // Этот тест вызываем в начале, чтобы посмотреть, что он не мешает продолжению работы программы
            new KeccakClearTest(tasks);
            // Замер производительности
            new KeccakSingleHashPerformanceTest(tasks);


            new EmtyString(tasks);
            new KeccakSimpleHashTest(tasks);
            new KeccakSimpleHashTestByBits(tasks);


            // --------------------------------------------------------------------------------
            // Завершающие тесты
            // --------------------------------------------------------------------------------
            new KeccakStatesArray_CountToCheck_test(tasks);
        }
    }
}
