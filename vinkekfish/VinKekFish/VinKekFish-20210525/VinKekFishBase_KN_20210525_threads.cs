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

namespace vinkekfish._20210525
{
    public unsafe partial class VinKekFishBase_KN_20210525: IDisposable
    {
        /// <summary>isEnded должен быть всегда false. Если true, то потоки завершают свою работу</summary>
        public volatile bool   isEnded = false;
        public readonly object sync    = new object();

        public void ThreadsFunction()
        {
            while (isEnded)
            {
                lock (sync)
                    Monitor.Wait(sync);
            }
        }

        /*
        public unsafe void DoThreefishForAllBlocks(byte * state1, byte * state2)
        {
            // Threefish_Static_Generated.Threefish1024_step(key: (ulong *) key, tweak: (ulong *) tweakTmp, text: (ulong *) cur);

            // tweakTmp[0] += 1;

            var po = new ParallelOptions();
            po.MaxDegreeOfParallelism = Environment.ProcessorCount;

            var pAddition = LenInThreeFish >> 1;
            var tweaks    = this.Tweaks;
            var matrix    = this.Matrix;

            Parallel.For
            (
                fromInclusive: 0, toExclusive: LenInThreeFish, parallelOptions: po,
                delegate (int position)
                {
                    var keyPosition = position + pAddition;
                    if (keyPosition > LenInThreeFish)
                        keyPosition -= LenInThreeFish;

                    var key = state1 + keyPosition * ThreeFishBlockLen;
                    Threefish_Static_Generated.Threefish1024_step
                    (
                        key:   (ulong *) key,
                        tweak: tweaks + position * CryptoTweakLen,
                        text:  (ulong *) cur
                    );
                }
            );*/
        }
    }
}

