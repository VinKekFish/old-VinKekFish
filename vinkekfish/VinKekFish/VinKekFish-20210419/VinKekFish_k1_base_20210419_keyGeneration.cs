using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vinkekfish.VinKekFish.VinKekFish_20210419
{
    public unsafe class VinKekFish_k1_base_20210419_keyGeneration: VinKekFish_k1_base_20210419
    {
        public VinKekFish_k1_base_20210419_keyGeneration()
        {
        }

        public override void Init1(int RoundsForTables, byte * additionalKeyForTables, long additionalKeyForTables_length, byte[] OpenInitVectorForTables = null, int PreRoundsForTranspose = 8)
        {
            base.Init1(RoundsForTables: RoundsForTables, additionalKeyForTables: additionalKeyForTables, additionalKeyForTables_length: additionalKeyForTables_length, OpenInitVectorForTables: OpenInitVectorForTables, PreRoundsForTranspose: PreRoundsForTranspose);
        }

        public override void Init2(byte * key, ulong key_length, byte[] OpenInitVector, ulong Rounds = 64, ulong RoundsForEnd = 64, ulong RoundsForExtendedKey = 64)
        {
            base.Init2(key: key, key_length: key_length, OpenInitVector: OpenInitVector, Rounds: Rounds, RoundsForEnd: RoundsForEnd, RoundsForExtendedKey: RoundsForExtendedKey);
        }

        public void GetKey()
        {
        }
    }
}
