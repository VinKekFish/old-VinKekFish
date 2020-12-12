using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeGenerated.Cryptoprimes;

namespace cryptoprime.VinKekFish
{
    public static unsafe class VinKekFishBase
    {
        public static void step(byte * key, byte * roundTweaks, byte * state, ushort * tablesForPermutations)
        {
            
        }

        public static int getNumberFromRing(int i, int ringModulo)
        {
            while (i < 0)
                i += ringModulo;

            while (i >= ringModulo)
                i -= ringModulo;

            return i;
        }

        const int ThreeFishBlockLen = 128;
        const int    KeccakBlockLen = 200;

        /// <summary>Применяет ThreeFish поблочно ко всему состоянию алгоритма</summary>
        /// <param name="beginCryptoState"></param>
        /// <param name="finalCryptoState"></param>
        /// <param name="tweak">Базовый tweak для раунда</param>
        /// <param name="len">Длина криптографического состояния в блоках ThreeFish1024 (по 128-мь байтов; ThreeFishBlockLen). len - нечётное</param>
        private static unsafe void DoThreefishForAllBlocks(byte* beginCryptoState, byte * finalCryptoState, byte * tweak, int len)
        {
            if ((len & 1) == 0)
                throw new ArgumentException("'len' must be odd", "len");
            
            byte* cur = finalCryptoState;
            byte* key = beginCryptoState;

            int j   = len >> 1;
            int add = 0;

            for (int i = 0; i < len; i++, j++, cur += ThreeFishBlockLen)
            {
                if (j >= len)
                    j = 0;

                add = j << 7; // blockLen * j;
                key = beginCryptoState + add;

                Threefish_Static_Generated.Threefish1024_step((ulong *) key, (ulong *) tweak, (ulong *) cur);
            }
        }

        /// <summary>Применяет к криптографическому состоянию CryptoState поблочное преобразование keccak</summary>
        /// <param name="CryptoState">Криптографическое состояние</param>
        /// <param name="len">Длина криптографического состояния в блоках keccak (длина по 200 байтов; KeccakBlockLen)</param>
        private static unsafe void DoKeccakForAllBlocks(byte* CryptoState, int len, ulong * b, ulong * c)
        {
            byte* cur = CryptoState;

            for (int i = 0; i < len; i++, cur += KeccakBlockLen)
            {
                keccak.Keccackf(a: (ulong *) cur, c: c, b: b);
            }
        }

        public static ushort[] transpose200_3200 = null;
        public static ushort[] transpose400_3200 = null;
        public static ushort[] transpose128_3200 = null;
        public static ushort[] transpose256_3200 = null;
        public static ushort[] transpose387_3200 = null;

        public static void GenTables()
        {
            if (transpose200_3200 != null)
                return;

            transpose200_3200 = GenTransposeTable(3200, 200);
            transpose400_3200 = GenTransposeTable(3200, 400);
            transpose128_3200 = GenTransposeTable(3200, 128);
            transpose256_3200 = GenTransposeTable(3200, 256);
            transpose387_3200 = GenTransposeTable(3200, 387);
        }

        public static ushort[] GenTransposeTable(ushort blockSize, ushort step, int numberOfRetries = 1)
        {
            var newTable = new ushort[blockSize];
            var buffer   = new ushort[blockSize];
            for (ushort i = 0; i < newTable.Length; i++)
            {
                newTable[i] = i;
                buffer  [i] = i;
            }

            for (int z = 0; z < numberOfRetries; z++)
            {
                ushort j = 0, k = 0;
                for (ushort i = 0; i < blockSize; i++)
                {
                    buffer[j++] = newTable[k];

                    k += step;
                    if (k >= blockSize)
                    {
                        k -= blockSize;
                        k++;
                    }
                }

                fixed (ushort* nt = newTable, buff = buffer)
                {
                    BytesBuilder.CopyTo(buffer.Length << 1, buffer.Length << 1, (byte*)buff, (byte*)nt);
                }
            }

            for (ushort i = 0; i < newTable.Length; i++)
            {
                if (!newTable.Contains(i))
                {
                    throw new Exception();
                }
            }

            return newTable;
        }
    }
}
