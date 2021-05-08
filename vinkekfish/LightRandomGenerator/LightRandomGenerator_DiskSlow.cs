using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using static cryptoprime.BytesBuilderForPointers;

namespace vinkekfish
{
    /// <summary>Класс, генерирующий некриптостойкие значения на основе времени дисковых операций. Это ОЧЕНЬ медленный генератор. Одно срабатывание зависит от того, что и как записывается внутри файловой системы. Может одно срабатывание давать за 1-5 секунд.</summary>
    public unsafe class LightRandomGenerator_DiskSlow: LightRandomGenerator
    {
        public LightRandomGenerator_DiskSlow(int CountToGenerate): base(CountToGenerate)
        {
        }

        /// <summary>На сколько будет засыпать пишущий поток (который делает приращения к счётчику)</summary>
        public int RSleepTimeout = 57;

        protected SortedList<string, FileSystemWatcher> watchers = new SortedList<string, FileSystemWatcher>(16);
        protected override void WriteThreadFunction(int CountToGenerate)
        {
            long lastLen = 0, curLen = 0, lastDrivesCount = 0, curDrivesCount = 0;
            DriveInfo[] disks;
            lastDrivesCount = CreateFileSystemWatchers();

            try
            {
                while (!ended)
                {
                    curLen         = 0;
                    curDrivesCount = 0;
                    disks          = DriveInfo.GetDrives();
                    for (int i = 0; i < disks.Length; i++)
                    {
                        try
                        {
                            curLen += disks[i].TotalFreeSpace;
                            curDrivesCount++;
                        }
                        catch
                        {
                            // Например, "Устройство не готово" (CD-носители без вставленных дисков)
                            curLen++;
                        }
                    }
                    curLen += disks.Length;

                    if (curDrivesCount != lastDrivesCount)
                    {
                        lastDrivesCount = CreateFileSystemWatchers();
                    }

                    if (curLen != lastLen)
                    {
                        lock (this)
                        {
                            curCNT++;               // Это теперь приращается многопоточно
                            lastLen = curLen;
                            Monitor.PulseAll(this);
                        }
                    }

                    lock (this)
                        Monitor.Wait(this, RSleepTimeout);

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

        public virtual long CreateFileSystemWatchers()
        {
            foreach (var w in watchers)
            {
                w.Value.EnableRaisingEvents = false;
                w.Value.Dispose();
            }

            watchers.Clear();

            long lastDrivesCount = 0;

            var disks = DriveInfo.GetDrives();
            for (int i = 0; i < disks.Length; i++)
            {
                if (!disks[i].IsReady)
                    continue;

                // Не создаём Watcher для съёмных устройств, т.к. он мешает их извлекать
                // if (disks[i].DriveType != DriveType.Removable)
                CreateSystemWatcher(disks[i].RootDirectory.FullName);

                lastDrivesCount++;
            }

            return lastDrivesCount;
        }

        public virtual void CreateSystemWatcher(string path)
        {
            try
            {
                var watcher = new FileSystemWatcher(path);
                watcher.Changed += A_Changed;
                watcher.Created += A_Changed;
                watcher.Deleted += A_Changed;
                watcher.Renamed += A_Changed;
                watcher.Error   += A_Error;

                watcher.IncludeSubdirectories = true;
                watcher.EnableRaisingEvents   = true;

                if (watchers.ContainsKey(path))
                {
                    watchers[path].EnableRaisingEvents = false;
                    watchers[path].Dispose();
                    watchers.Remove(path);
                }

                watchers.Add(path, watcher);
            }
            catch
            {
                curCNT++;
            }
        }

        public virtual void A_Error(object sender, ErrorEventArgs e)
        {
            curCNT++;
        }

        public string lastChangedFileName = "";
        public virtual void A_Changed(object sender, FileSystemEventArgs e)
        {
            lock (this)
            {
                curCNT++;

                if (e.ChangeType != WatcherChangeTypes.Changed)
                {
                    Monitor.PulseAll(this);
                }
                else
                {
                    // Защита от сработок на один и тот же файл (если он раз за разом быстро меняется)
                    if (e.FullPath != lastChangedFileName)
                    {
                        lastChangedFileName = e.FullPath;
                        Monitor.PulseAll(this);
                    }
                }
            }
        }

        public override void ReadThreadFunction(int CountToGenerate)
        {
            try
            {
                while (!ended)
                {
                    if (doSleepR)
                        Thread.Sleep(0);

                    lock (this)
                    while (lastCNT == curCNT)
                    {
                        Monitor.Wait(this);
                    }

                    lastCNT = curCNT;

                    lock (this)
                    {
                        // В массив длиной, не кратной 3, перезапись будет происходить со смещением
                        // curCNT даже не записываем, т.к. он, скорее всего, приращается на единицу
                        var bt  = new byte[3];
                        var now = DateTime.Now.Ticks;
                        bt[0] = (byte) now; now >>= 8;
                        bt[1] = (byte) now; now >>= 8;
                        bt[2] = (byte) now;

                        if (GeneratedCount < CountToGenerate)
                        {
                            for (int i = 0; i < bt.Length; i++)
                            {
                                // При изменении, ниже также изменять
                                GeneratedBytes.array[(GeneratedCount + StartOfGenerated) % CountToGenerate] = bt[i];
                                // GeneratedBytes.array[(GeneratedCount + StartOfGenerated) % CountToGenerate] = (byte) curCNT;
                                GeneratedCount++;
                            }
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
                                for (int i = 0; i < bt.Length; i++)
                                {
                                    StartOfGenerated++;
                                    if (StartOfGenerated >= CountToGenerate)
                                        StartOfGenerated = 0;

                                    GeneratedBytes.array[(GeneratedCount + StartOfGenerated) % CountToGenerate] += bt[i];
                                }
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

        /// <summary>Очищает объект</summary>
        /// <param name="disposing"><see langword="true"/> во всех случаях, кроме вызова из деструктора</param>
        public override void Dispose(bool disposing)
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
        }
    }
}
