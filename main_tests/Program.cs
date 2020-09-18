using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using vinkekfish;
using System.Diagnostics;

namespace main_tests
{
    // Добавление новых тестов см. в файле Program_AddTasks.cs, метод AddTasks
    // Программа просто консольно запускается и выполняет написанные тесты многопоточно
    // Подсчитывает количество ошибок
    partial class Program
    {
        static readonly string LogFileNameTempl = "tests-$.log";
        static          string LogFileName      = null;
        static int Main(string[] args)
        {
            var now = DateTime.Now;
            LogFileName = LogFileNameTempl.Replace("$", HelperClass.DateToDateFileString(now));
            File.WriteAllText  (LogFileName,   HelperClass.DateToDateString(now) + "\nArgs:\n");
            File.AppendAllLines(LogFileName, args);
            File.AppendAllText (LogFileName, "\n");

            System.Collections.Concurrent.ConcurrentQueue<TestTask> tasks = new ConcurrentQueue<TestTask>();
            AddTasks(tasks);

            Object sync = new Object();
            int started = 0;            // Количество запущенных прямо сейчас задач
            int ended   = 0;            // Количество завершённых задач
            int errored = 0;            // Количество задач, завершённых с ошибкой
            int PC = Environment.ProcessorCount;
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
                            task.error.Add(new Error() { ex = e, Message = "During the test the exception occured" });
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
                            File.AppendAllText(LogFileName, "task " + task.Name + "\n");
                            File.AppendAllText(LogFileName, "task started at " + HelperClass.DateToDateString(task.started) + "\n");
                            File.AppendAllText(LogFileName, "task ended   at " + HelperClass.DateToDateString(task.endTime) + "\n\n");
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
            {
                Console.WriteLine("Press 'Enter' to exit");
                Console.ReadLine();
            }

            return errored;

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
                    now = DateTime.Now;
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
