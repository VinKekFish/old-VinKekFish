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
        public          bool        ended = false;
        public readonly List<Error> error = new List<Error>();

        public DateTime started = default(DateTime);
        public DateTime endTime = default(DateTime);
    }

    public delegate void TestTaskFn();
    // public delegate void AddTestFn (ConcurrentQueue<TestTask> tasks);

    class Program
    {
        static void Main(string[] args)
        {
            System.Collections.Concurrent.ConcurrentQueue<TestTask> tasks = new ConcurrentQueue<TestTask>();

            new EmtyString(tasks);

            Object sync = new Object();
            int started = 0;            // Количество запущенных прямо сейчас задач
            int ended   = 0;            // Количество завершённых задач
            int errored = 0;            // Количество задач, завершённых с ошибкой
            int PC      = Environment.ProcessorCount;
            foreach (var task in tasks)
            {
                Interlocked.Increment(ref started);
                ThreadPool.QueueUserWorkItem
                (
                    delegate
                    {
                        try
                        {
                            task.started = DateTime.Now;
                            task.task();
                        }
                        catch (Exception e)
                        {
                            task.error.Add(new Error() {ex = e, Message = "During the test the exception occured"});
                        }
                        finally
                        {
                            Interlocked.Decrement(ref started);
                            Interlocked.Increment(ref ended);
                            task.ended = true;

                            if (task.error.Count > 0)
                                Interlocked.Increment(ref errored);

                            lock (sync)
                                Monitor.PulseAll(sync);

                            task.endTime = DateTime.Now;
                        }
                    }
                );

                while (started >= PC)
                    lock (sync)
                    {
                        Monitor.Wait(sync, 2000);
                        WaitMessages();
                    }
            }

            while (started > 0)
                lock (sync)
                {
                    Monitor.Wait(sync, 2000);
                    WaitMessages(true);
                }

            WaitMessages(false, true);
            if (args.Length == 0)
                Console.ReadLine();

            void WaitMessages(bool showWaitTasks = false, bool endedAllTasks = false)
            {
                Console.Clear();
                // Console.CursorLeft = 0;
                // Console.CursorTop  = 0;
                Console.WriteLine("Выполнено/всего: " + ended + " / " + tasks.Count);
                Console.WriteLine("Задачи с ошибокй: " + errored);
                Console.WriteLine();

                if (showWaitTasks && ended != tasks.Count)
                {
                    var now = DateTime.Now;
                    Console.WriteLine("Выполняемые задачи: ");
                    Console.WriteLine();
                    foreach (var task in tasks)
                    {
                        if (!task.ended)
                        {
                            Console.WriteLine(task.Name + ": " + (now - task.started).ToString(@"hh\:mm\:ss"));
                        }
                    }
                }

                if (endedAllTasks)
                {
                    foreach (var task in tasks)
                    {
                        if (task.error.Count > 0)
                        {
                            Console.WriteLine();
                            Console.WriteLine("For task " + task.Name);
                            foreach (var e in task.error)
                            {
                                Console.WriteLine(e.Message);
                            }
                        }
                    }
                }
            }
        }
    }
}
