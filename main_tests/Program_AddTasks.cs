// #define doPerformaceTest

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

        public DateTime started = default;
        public DateTime endTime = default;

        public bool waitBefore = false;
        public bool waitAfter  = false;

        /// <summary>Выполнение задачи в процентах (0-100)</summary>
        public float done = 0f;
    }

    public delegate void TestTaskFn();

    partial class Program
    {
        public delegate void AddTasksDelegate(ConcurrentQueue<TestTask> tasks);
        public static AddTasksDelegate AddTasksFunc = AddTasks;
        protected static void AddTasks(ConcurrentQueue<TestTask> tasks)
        {
            #if doPerformaceTest
                new ThreefishPerformanceTest(tasks);
            #else

            // Это делаем однопоточно, чтобы точно не помешать другим потомкам, т.к. это, по сути, аварийное выделение памяти
            // Этот тест вызываем в начале, чтобы посмотреть, что он не мешает продолжению работы программы
            // new KeccakClearTest(tasks);
            
            // Раскомментировать, если нужно
            // Замер производительности, тоже однопоточный тест
            // new KeccakSingleHashPerformanceTest(tasks);

            // Тесты криптографии и функции очистки строк
            /*
            new EmtyString(tasks);
            new KeccakSimpleHashTest(tasks);
            new KeccakSimpleHashTestByBits(tasks);
            new ThreeFishTestByBits   (tasks);
            new ThreeFishGenTestByBits(tasks);
            */
            new LightRandomGenerator_test01(tasks);

            // --------------------------------------------------------------------------------
            // Завершающие тесты
            // --------------------------------------------------------------------------------
            // Это проверка на то, что все зафиксированные блоки в KeccakSimpleHashTest* были удалены
            new KeccakStatesArray_CountToCheck_test(tasks);

            #endif
        }
    }
}
