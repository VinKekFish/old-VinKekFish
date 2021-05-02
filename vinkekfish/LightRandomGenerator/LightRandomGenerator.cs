using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using static cryptoprime.BytesBuilderForPointers;

namespace vinkekfish.LightRandomGenerator
{
    /// <summary>Класс, генерирующий некриптостойкие значения на основе ожидания потоков</summary>
    public unsafe class LightRandomGenerator: IDisposable
    {
        public    volatile bool    ended  = false;
        protected readonly Thread rthread = null;
        protected readonly Thread wthread = null;

        protected volatile ushort  curCNT = 0;
        protected volatile ushort lastCNT = 0;
        public LightRandomGenerator(int CountToGenerate)
        {
            var allocator  = new AllocHGlobal_AllocatorForUnsafeMemory();
            GeneratedBytes = allocator.AllocMemory(CountToGenerate);

            wthread = new Thread
            (
                delegate()
                {
                    try
                    {
                        while (!ended)
                        {
                            curCNT++;

                            // Временный останов
                            if (GeneratedCount >= CountToGenerate)
                            lock (this)
                            {
                                Monitor.Wait(this);
                            }
                        }
                    }
                    finally
                    {
                        Interlocked.Decrement(ref isEnded);
                        lock (this)
                        {
                            Monitor.PulseAll(this);
                        }
                    }
                }
            );

            rthread = new Thread
            (
                delegate()
                {
                    try
                    {
                        while (!ended)
                        {
                            while (lastCNT == curCNT)
                                Thread.Sleep(0);

                            lastCNT = curCNT;

                            lock (this)
                            {
                                if (GeneratedCount < CountToGenerate)
                                {
                                    GeneratedBytes.array[(GeneratedCount + StartOfGenerated) % CountToGenerate] = (byte) curCNT;
                                    GeneratedCount++;
                                }
                                else
                                {
                                    wthread.Priority = ThreadPriority.Lowest;
                                    Monitor.PulseAll(this);
                                    Monitor.Wait(this, 1000);
                                }
                            }
                        }
                    }
                    finally
                    {
                        Interlocked.Decrement(ref isEnded);
                        lock (this)
                        {
                            Monitor.PulseAll(this);
                        }
                    }
                }
            );

            wthread.Priority = ThreadPriority.Lowest;
            rthread.Priority = ThreadPriority.Lowest;
        }

        protected readonly   Record GeneratedBytes   = null;
        protected readonly   int    CountToGenerate  = 0;
        protected volatile   int    GeneratedCount   = 0;
        protected volatile   int    StartOfGenerated = 0;
        protected volatile   int    isEnded          = 2;

        /// <summary>Сбрасывает все сгенерированные байты без полезного использования</summary>
        public void ResetGeneratedBytes()
        {
            StartOfGenerated = 0;
            GeneratedCount   = 0;

            wthread.Priority = ThreadPriority.Normal;
            lock (this)
                Monitor.PulseAll(this);
        }

        /// <summary>Получает из генератора псевдослучайные некриптостойкие байты</summary>
        /// <param name="result">Некриптостойкий результат</param>
        public void GetRandomBytes(byte[] result)
        {
            if (ended)
                throw new Exception("LightRandomGenerator.GetRandomBytes: LightRandomGenerator is end of work");

            if (result.Length > CountToGenerate)
                throw new ArgumentOutOfRangeException("LightRandomGenerator.GetRandomBytes: result.Length > CountToGenerate");
            
            WaitForGenerator(result);

            lock (this)
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = GeneratedBytes.array[StartOfGenerated];

                StartOfGenerated++;
                GeneratedCount--;
                if (StartOfGenerated >= CountToGenerate)
                    StartOfGenerated = 0;
            }

            wthread.Priority = ThreadPriority.Normal;
            lock (this)
                Monitor.PulseAll(this);
        }

        private void WaitForGenerator(byte[] result)
        {
            lock (this)
            {
                while (GeneratedCount < result.Length)
                {
                    Monitor.PulseAll(this);
                    Monitor.Wait(this, 1000);
                }
            }
        }

        public void Dispose()
        {
            ended = true;

            lock (this)
            {
                while (isEnded > 0)
                {
                    Monitor.PulseAll(this);
                    Monitor.Wait(this, 100);
                }

                GeneratedBytes.Dispose();
            }
            keccak.keccak_20200918.Keccak_PRNG_20201128.AllocFullMemoryInLongBlocksWithSecondAlloc();
        }
    }
}
