using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using static cryptoprime.BytesBuilderForPointers;

namespace vinkekfish
{
    /// <summary>Этот класс для тестов. Нет необходимости его использовать как-то ещё. curCNT не приращается, только curCNT_PM - это всё различие</summary>
    public unsafe class LightRandomGenerator_forTests: LightRandomGenerator
    {
        public LightRandomGenerator_forTests(int CountToGenerate): base(CountToGenerate)
        {
        }

        public override void ReadThreadFunction(int CountToGenerate)
        {
            try
            {
                while (!ended)
                {
                    if (doSleepR)
                        Thread.Sleep(0);

                    while (lastCNT == curCNT_PM)
                        Thread.Sleep(0);

                    lastCNT = curCNT_PM;

                    lock (this)
                    {
                        var out0 = curCNT + curCNT_PM;
                        var out8 = out0 >> 8;
                        if (GeneratedCount < CountToGenerate)
                        {
                            // При изменении, ниже также изменять
                            // На всякий случай делаем xor между младшим и старшим байтом, чтобы все биты были учтены
                            // Не такая уж хорошая статистика получается по младшим байтам, как могло бы быть
                            GeneratedBytes.array[(GeneratedCount + StartOfGenerated) % CountToGenerate] = (byte)(out0 ^ out8);
                            // GeneratedBytes.array[(GeneratedCount + StartOfGenerated) % CountToGenerate] = (byte) curCNT;
                            GeneratedCount++;
                        }
                        else
                        {
                            SetThreadsPriority(ThreadPriority.Lowest);
                            if (doWaitR || doWaitW)
                            {
                                Monitor.PulseAll(this);
                                Monitor.Wait(this);
                            }
                            else
                            {
                                StartOfGenerated++;
                                if (StartOfGenerated >= CountToGenerate)
                                    StartOfGenerated = 0;

                                GeneratedBytes.array[(GeneratedCount + StartOfGenerated) % CountToGenerate] += (byte)(out0 ^ out8);
                            }
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

        protected override void Write1ThreadFunction(int CountToGenerate)
        {
            try
            {
                while (!ended)
                {
                    // curCNT++;
                    curCNT_PM++;        // ВНИМАНИЕ! Здесь специально сделан небезопасный инкремент!

                    // Временный останов
                    if (doWaitW && GeneratedCount >= CountToGenerate)
                    {
                        lock (this)
                        {
                            Monitor.Wait(this);
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

        protected override void Write2ThreadFunction(int CountToGenerate)
        {
            try
            {
                while (!ended)
                {
                    curCNT_PM--;            // ВНИМАНИЕ! Здесь специально сделан небезопасный декремент!

                    // Временный останов
                    if (doWaitW && GeneratedCount >= CountToGenerate)
                    {
                        lock (this)
                        {
                            Monitor.Wait(this);
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
    }
}
