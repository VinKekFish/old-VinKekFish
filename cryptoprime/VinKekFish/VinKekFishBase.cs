﻿using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cryptoprime.VinKekFish
{
    public static unsafe class VinKekFishBase
    {
        public static void step(byte * key, byte * roundTweaks, byte * state, ushort * tablesForPermutations)
        {
            
        }

        private static unsafe void DoThreefishForAllBlocks(byte* msgSource, byte * msgToEncrypt, byte * tweak, long len, int blockLen, int step)
        {
            byte* cur = msgSource;
            for (int i = 0; i <= len - blockLen; i += blockLen)
            {
                var key = cur + 128;
                if (i == len - blockLen*step)
                    key = msgToEncrypt;

                CodeGenerated.Cryptoprimes.Threefish_Static_Generated.Threefish1024_step((ulong *) key, (ulong *) tweak, (ulong *) cur);

                cur += blockLen;
            }
        }

        public const int MaxTableNumber = 16;
        public static readonly ushort[][] tables64_7 = new ushort[MaxTableNumber][];
        public static readonly ushort[][] tables64_1 = new ushort[MaxTableNumber][];
        public static readonly ushort [,] tables     = new ushort[32, BlockSize];

        public const int BlockSize = 2048;

        public static readonly ushort[] valueToAdd = {1, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71, 73, 79, 83, 89, 97, 101, 103, 107, 109, 113, 127, 131, 137};
        public static void GenerateTables()
        {
            for (int i = 0; i < MaxTableNumber; i++)
                tables64_7[i] = GenBaseTable(64, i+1, 7);

            for (int i = 1; i <= MaxTableNumber; i++)
                tables64_1[i] = GenBaseTable(64, i+1, 1);

            for (int i = 0; i < MaxTableNumber; i++)
            {
                var add = BlockSize * MaxTableNumber;

                fixed (ushort * ts = tables, s = tables64_1[i])
                    BytesBuilder.CopyTo(BlockSize, BlockSize, (byte *) s, (byte *) (ts + i*BlockSize));

                fixed (ushort * ts = tables, s = tables64_7[i])
                    BytesBuilder.CopyTo(BlockSize, BlockSize, (byte *) s, (byte *) (ts + add + i*BlockSize));
            }
        }

        public static ushort[] GenBaseTable(int blockSize, int step, int numberOfRetries = 1)
        {
            var newTable = new ushort[BlockSize];
            var buffer   = new ushort[BlockSize];
            for (ushort i = 0; i < newTable.Length; i++)
            {
                newTable[i] = i;
                buffer[i] = i;
            }

            for (int z = 0; z < numberOfRetries; z++)
            {
                ushort j = 0, k = 0;
                for (ushort i = 0; i < newTable.Length; i++)
                {
                    buffer[j++] = newTable[k];

                    k += (ushort) (blockSize + step);
                    if (k >= buffer.Length)
                    {
                        k -= (ushort)buffer.Length;
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
