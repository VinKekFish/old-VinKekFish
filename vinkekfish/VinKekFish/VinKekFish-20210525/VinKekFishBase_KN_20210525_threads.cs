using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using CodeGenerated.Cryptoprimes;

using cryptoprime;
using cryptoprime.VinKekFish;

using static cryptoprime.BytesBuilderForPointers;
using static cryptoprime.VinKekFish.VinKekFishBase_etalonK1;

namespace vinkekfish
{
    public unsafe partial class VinKekFishBase_KN_20210525
    {
        protected readonly Thread[] threads = null;

        /// <summary>isEnded должен быть всегда false. Если true, то потоки завершают свою работу</summary>
        public volatile bool   isEnded = false;
        public readonly object sync    = new object();
                                                                                    /// <summary>Типовые задания для выполнения. Инициализируются в конструкторе.</summary>
        protected readonly ThreadStart[]   ThreadsFunc               = null;        /// <summary>Номер функции, которая должна быть выполнена в этой задаче</summary>
        protected volatile int             ThreadsFunc_CurrentNumber = 0;           /// <summary>Приращается каждый раз, когда ставится на исполнение новая задача</summary>
        protected volatile int             ThreadsTask_CountOfTasks  = 0;           /// <summary>Количество потоков, ещё не исполнивших эту задачу</summary>
        protected volatile int             ThreadsExecutedForTask    = 0;
                                                                                    /// <summary>Исключения, свершившиеся при исполнении задач</summary>
        protected readonly ConcurrentQueue <Exception> ThreadsTask_Errors      = new ConcurrentQueue<Exception>();      /// <summary>Количество исключений, свершившихся при выполнении задач</summary>
        protected volatile int                         ThreadsTask_ErrorsCount = 0;
                                                                                    /// <summary>Количество потоков, которое не находися в ожидании</summary>
        protected volatile int           ThreadsInFunc   = 0;                       /// <summary>Количество запущенных потоков</summary>
        protected volatile int           ThreadsExecuted = 0;

        protected virtual void ThreadsFunction()
        {
            try
            {
                Interlocked.Increment(ref ThreadsExecuted);
                int Task_CountOfTasks;

                while (!isEnded)
                {
                    Task_CountOfTasks = ThreadsTask_CountOfTasks;
                    try
                    {
                        ThreadsFunc[ThreadsFunc_CurrentNumber]();
                    }
                    catch (Exception ex)
                    {
                        ThreadsTask_Errors.Append(ex);
                        Interlocked.Increment(ref ThreadsTask_ErrorsCount);
                    }

                    lock (sync)
                    {
                        ThreadsInFunc--;                                            // Это обязательно в блокировке sync
                        ThreadsExecutedForTask--;                                   // Это обязательно в блокировке sync
                        Monitor.PulseAll(sync);

                        while (Task_CountOfTasks == ThreadsTask_CountOfTasks)       // Это обязательно в блокировке sync
                        {
                            if (isEnded)
                                goto EndThread;

                            Monitor.Wait(sync);
                        }
                        ThreadsInFunc++;                                            // Это обязательно в блокировке sync
                    }
                }

                EndThread: ;
            }
            catch (Exception ex)
            {
                ThreadsTask_Errors.Append(ex);
                Interlocked.Increment(ref ThreadsTask_ErrorsCount);
            }
            finally
            {
                Interlocked.Decrement(ref ThreadsExecuted);

                lock (sync)
                {
                    Monitor.PulseAll(sync);
                }
            }
        }

        protected void ThreadFunction_empty()
        {}
                                                    /// <summary>Вызывается для ожидания выполнения всех потоков перед тем, как ставить новую задачу или брать результат</summary>
        protected virtual void waitForDoFunction()
        {
            lock (sync)
            {
                while (ThreadsExecutedForTask > 0 && ThreadsExecuted > 0)
                    Monitor.Wait(sync);
            }
        }
                                                    /// <summary>Вызывается после waitForDoFunction для постановки новой задачи после инициализации её параметров. Пример, см. в функции doKeccak()</summary>
        protected virtual void doFunction(int ThreadsFunc_CurrentNumber)
        {
            lock (sync)
            {
                if (ThreadsExecutedForTask > 0 || ThreadsExecuted <= 0)
                    throw new Exception("VinKekFishBase_KN_20210525.doFunction: ThreadsExecutedForTask > 0 || ThreadsExecuted <= 0");

                ThreadsTask_CountOfTasks++;             // Это обязательно в блокировке sync
                ThreadsExecutedForTask = ThreadCount;   // Это обязательно в блокировке sync

                this.ThreadsFunc_CurrentNumber = ThreadsFunc_CurrentNumber;
                Monitor.PulseAll(sync);
            }
        }

        /// <summary>Запускает многопоточную поблочную обработку функцией keccak</summary>
        protected void doKeccak()
        {
            waitForDoFunction();                // Ждём конца выполнения предыдущей задачи

            CurrentKeccakBlockNumber[0] = 0;
            CurrentKeccakBlockNumber[1] = 1;

            doFunction(1);
        }
                                                                                            /// <summary>Массив счётчика блоков для определения текущего блока для обработки keccak. [0] - чётные элементы, [1] - нечётные элементы</summary>
        protected volatile int[] CurrentKeccakBlockNumber = {0, 1};
        protected void ThreadFunction_Keccak()
        {
            for (int i = 0; i <= 1; i++)
            {
                do
                {
                    var index  = Interlocked.Add(ref CurrentKeccakBlockNumber[i], 2) - 2;
                    if (index >= LenInKeccak)
                    {
                        break;
                    }

                    var offset = KeccakBlockLen * index;
                    var off1   = st1 + offset;
                    var off2   = st2 + offset;

                    byte * mat = Matrix +  MatrixLen * index;
                    byte * off = off1;

                    if (i == 0)
                    {
                        BytesBuilder.CopyTo(KeccakBlockLen, KeccakBlockLen, off1, off2);
                        off = off2;
                    }

                    keccak.Keccackf(a: (ulong *) off, c: (ulong *) (mat + keccak.b_size), b: (ulong *) mat);

                    if (i != 0)
                    {
                        BytesBuilder.CopyTo(KeccakBlockLen, KeccakBlockLen, off1, off2);
                    }
                }
                while (true);
            }
        }

        protected volatile int CurrentThreeFishBlockNumber = 0;
        protected void ThreadFunction_ThreeFish()
        {
        }

        protected volatile int CurrentPermutationBlockNumber = 0;
        protected void ThreadFunction_Permutation()
        {
        }
    }
}

