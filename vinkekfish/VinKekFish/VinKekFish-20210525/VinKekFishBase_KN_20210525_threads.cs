using System;
using System.Collections.Generic;
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

        protected readonly ThreadStart[] ThreadsFunc = null;
        protected volatile int           ThreadsFunc_CurrentNumber = 0;

        protected volatile int           ThreadsInFunc = 0;

        protected virtual void ThreadsFunction()
        {
            while (!isEnded)
            {
                Interlocked.Increment(ref ThreadsInFunc);
                try
                {
                    ThreadsFunc[ThreadsFunc_CurrentNumber]();
                }
                finally
                {
                    Interlocked.Decrement(ref ThreadsInFunc);
                }

                lock (sync)
                {
                    if (isEnded)
                        break;

                    if (ThreadsInFunc == 0)
                        Monitor.PulseAll(sync);

                    if (ThreadsFunc_CurrentNumber == 0)
                        Monitor.Wait(sync);
                }
            }

            lock (sync)
                Monitor.PulseAll(sync);
        }

        protected void ThreadFunction_empty()
        {}

        protected void doKeccak()
        {
            CurrentKeccakBlockNumber  = 0;
            ThreadsFunc_CurrentNumber = 1;

            lock (sync)
                Monitor.PulseAll(sync);
        }

        protected volatile int CurrentKeccakBlockNumber = 0;
        protected void ThreadFunction_Keccak()
        {
            do
            {
                var index  = Interlocked.Increment(ref CurrentKeccakBlockNumber) - 1;
                if (index >= LenInKeccak)
                    return;

                var offset = KeccakBlockLen * index;
                var off1   = st1 + offset;
                var off2   = st2 + offset;
                var odd    = (index & 1) > 0;

                byte * mat = Matrix +  MatrixLen * index;
                byte * off = off1;
                if (odd)
                {
                    BytesBuilder.CopyTo(KeccakBlockLen, KeccakBlockLen, off1, off2);
                    off = off2;
                }

                keccak.Keccackf(a: (ulong *) off, c: (ulong *) (mat + keccak.b_size), b: (ulong *) mat);

                if (!odd)
                {
                    BytesBuilder.CopyTo(KeccakBlockLen, KeccakBlockLen, off1, off2);
                }
            }
            while (true);
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

