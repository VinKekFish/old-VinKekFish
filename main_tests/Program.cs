using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace main_tests
{
    public class Error
    {
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
        public          bool        ended = false;
        public readonly List<Error> error = new List<Error>();
    }

    public delegate void TestTaskFn();
    // public delegate void AddTestFn (ConcurrentQueue<TestTask> tasks);

    class Program
    {
        static void Main(string[] args)
        {
            System.Collections.Concurrent.ConcurrentQueue<TestTask> tasks = new ConcurrentQueue<TestTask>();



            if (args.Length == 0)
                Console.ReadLine();
        }
    }
}
