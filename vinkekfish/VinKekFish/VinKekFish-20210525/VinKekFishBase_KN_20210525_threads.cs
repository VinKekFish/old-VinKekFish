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
        protected volatile bool   isEnded = false;
        public    readonly object sync    = new object();
                                                                                    /// <summary>Функция, которая должна быть выполнена в этой задаче</summary>
        protected volatile ThreadStart     ThreadsFunc_Current       = null;        /// <summary>Приращается каждый раз, когда ставится на исполнение новая задача</summary><remarks>Обязательно в lock (sync)</remarks>
        protected volatile int             ThreadsTask_CountOfTasks  = 0;           /// <summary>Количество потоков, ещё не исполнивших эту задачу</summary><remarks>Обязательно в lock (sync)</remarks>
        protected volatile int             ThreadsExecutedForTask    = 0;
                                                                                    /// <summary>Исключения, свершившиеся при исполнении задач</summary>
        protected readonly ConcurrentQueue <Exception> ThreadsTask_Errors      = new ConcurrentQueue<Exception>();      /// <summary>Количество исключений, свершившихся при выполнении задач</summary>
        protected volatile int                         ThreadsTask_ErrorsCount = 0;
                                                                                    /// <summary>Количество потоков, которое не находися в ожидании.</summary><remarks>Обязательно в lock (sync)</remarks>
        protected volatile int           ThreadsInFunc   = 0;                       /// <summary>Количество запущенных потоков</summary>
        protected volatile int           ThreadsExecuted = 0;

        public bool IsEnded
        {
            get => isEnded || ThreadsExecuted <= 0;
            set
            {
                if (value)
                    isEnded = true;
            }
        }

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
                        ThreadsFunc_Current();
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

                            Monitor.Wait(sync); // TODO: подумать насчёт доступа к памяти
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
                while (IsTaskExecuted)
                    Monitor.Wait(sync);
            }
        }
                                                                                                    /// <summary>Если <see langword="true"/>, значит, выполняется задача. Постановка другой задачи невозможна, пока эта не будет закончена</summary>
        public bool IsTaskExecuted => ThreadsExecutedForTask > 0 && ThreadsExecuted > 0;

                                                    /// <summary>Вызывается после waitForDoFunction для постановки новой задачи после инициализации её параметров. Пример, см. в функции doKeccak()</summary>
        protected virtual void doFunction(ThreadStart ThreadFunc)
        {
            lock (sync)
            {
                if (ThreadsExecutedForTask > 0 || ThreadsExecuted <= 0)
                    throw new Exception("VinKekFishBase_KN_20210525.doFunction: ThreadsExecutedForTask > 0 || ThreadsExecuted <= 0");

                ThreadsTask_CountOfTasks++;             // Это обязательно в блокировке sync
                ThreadsExecutedForTask = ThreadCount;   // Это обязательно в блокировке sync

                ThreadsFunc_Current = ThreadFunc;
                Monitor.PulseAll(sync);
            }
        }

        /// <summary>Запускает многопоточную поблочную обработку алгоритмом keccak</summary>
        protected void doKeccak()
        {
            waitForDoFunction();                // Ждём конца выполнения предыдущей задачи

            CurrentKeccakBlockNumber[0] = 0;
            CurrentKeccakBlockNumber[1] = 1;

            doFunction(ThreadFunction_Keccak);
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

        /// <summary>Запускает многопоточную поблочную обработку алгоритмом ThreeFish</summary>
        protected void doThreeFish()
        {
            waitForDoFunction();                // Ждём конца выполнения предыдущей задачи

            CurrentThreeFishBlockNumber = 0;
            BytesBuilder.CopyTo(Len, Len, st1, st2);

            doFunction(ThreadFunction_ThreeFish);
        }

        protected volatile int CurrentThreeFishBlockNumber = 0;
        protected void ThreadFunction_ThreeFish()
        {
            do
            {
                var index  = Interlocked.Increment(ref CurrentThreeFishBlockNumber) - 1;
                if (index >= LenInThreeFish)
                {
                    break;
                }

                var offsetC = ThreeFishBlockLen * index;
                var offsetK = ThreeFishBlockLen * NumbersOfThreeFishBlocks[index];
                var tweaks  = (ulong *) (((byte *) Tweaks) + CryptoTweakLen * index);
                var offC    = st2 + offsetC;
                var offK    = st1 + offsetK;
                tweaks[0]  += (uint) index;

                BytesBuilder.CopyTo(CryptoTweakLen, CryptoTweakLen, (byte *) Tweaks, (byte *) tweaks);

                Threefish_Static_Generated.Threefish1024_step(key: (ulong *) offK, tweak: tweaks, text: (ulong *) offC);
            }
            while (true);
        }

        /// <summary>Запускает многопоточную поблочную перестановку</summary>
        protected void doPermutation()
        {
            waitForDoFunction();                // Ждём конца выполнения предыдущей задачи

            CurrentPermutationBlockNumber = 0;

            doFunction(ThreadFunction_Permutation);
        }

        protected volatile int      CurrentPermutationBlockNumber = 0;
        protected volatile ushort * CurrentPermutationTable       = null;
        protected void ThreadFunction_Permutation()
        {
            do
            {
                var index  = Interlocked.Increment(ref CurrentPermutationBlockNumber) - 1;
                if (index >= LenInThreadBlock)          // Len всегда кратно LenInThreadBlock, см. конструктор
                {
                    break;
                }

                var table  = CurrentPermutationTable;
                var offset = LenThreadBlock * index;
                var off1   = st1 + offset;
                var off2   = st2 + offset;

                for (int i = 0; i < LenThreadBlock; i++)
                {
                    off2[i] = off1[  table[i]  ];
                }

            }
            while (true);
        }
                                                                    /// <summary>Попусту теребит память: это простая защита от выгрузки памяти в файл подкачки</summary>
        protected void WaitFunction(object state)
        {
            if (IsTaskExecuted)
                return;

            lock (sync)
            {
                if (!IsTaskExecuted)
                    BlankRead();
            }
        }
                                                                    /// <summary>Размер страницы оперативной памяти</summary>
        public const int PAGE_SIZE = 4096;
                                                                    /// <summary>Попусту теребит память: это простая защита от выгрузки памяти в файл подкачки</summary>
        protected void BlankRead()
        {
            for (int i = 0; i < Len; i += PAGE_SIZE)
            {
                var a = State1[i];
            }

            lock (this)
            {
                var len = tablesForPermutations.len;
                var a   = tablesForPermutations.array;
                for (int i = 0; i < len; i += PAGE_SIZE)
                {
                    var b = a[i];
                }
            }
        }
    }
}

