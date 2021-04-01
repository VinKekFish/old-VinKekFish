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
        const int CryptoStateLen          = 3200; // В байтах
        const int CryptoStateLenKeccak    = CryptoStateLen / KeccakBlockLen;
        const int CryptoStateLenThreeFish = CryptoStateLen / ThreeFishBlockLen;

        /// <summary>Шаг алгоритма ПОСЛЕ ввода данных</summary>
        /// <param name="countOfRounds">Количество раундов</param>
        /// <param name="tweak">Tweak после ввода данных, 16 байтов (все массивы могут быть в одном, если это удобно). Не изменяется в функции.</param>
        /// <param name="tweakTmp">Дополнительный массив для временного tweak, 16 байтов. Изменяется в функции.</param>
        /// <param name="tweakTmp2">Дополнительный массив для временного tweak, 16 байтов. Изменяется в функции.</param>
        /// /// <param name="tweakTmp3">Дополнительный массив для временного tweak, 16 байтов. Изменяется в функции.</param>
        /// <param name="state">Криптографическое состояние</param>
        /// <param name="state2">Второй массив для криптографического состояния</param>
        /// <param name="tablesForPermutations">Массив таблиц перестановок на каждый раунд. Длина должна быть countOfRounds*4 (*CryptoStateLen*ushort на каждую таблицу)</param>
        /// <param name="b">Вспомогательный массив b для keccak.Keccackf</param>
        /// <param name="c">Вспомогательный массив c для keccak.Keccackf</param>
        public static void step(ulong countOfRounds, ulong * tweak, ulong * tweakTmp, ulong * tweakTmp2, byte * state, byte * state2, ushort * tablesForPermutations, byte* b, byte* c, ushort * transpose200_3200)
        {
            tweakTmp[0] = tweak[0];
            tweakTmp[1] = tweak[1];

            for (ulong round = 0; round < countOfRounds; round++)
            {
                DoKeccakForAllBlocks(state, CryptoStateLenKeccak, b: (ulong*) b, c: (ulong*) c);
                DoPermutation(state, state2, CryptoStateLen, tablesForPermutations);
                tablesForPermutations += CryptoStateLen;

                DoThreefishForAllBlocks(state2, state, tweakTmp, tweakTmp2);
                DoPermutation(state, state2, CryptoStateLen, tablesForPermutations);
                tablesForPermutations += CryptoStateLen;

                // Довычисление tweakVal для второго преобразования VinKekFish
                tweakTmp[0] += 0x1_0000_0000U;

                DoKeccakForAllBlocks(state2, CryptoStateLenKeccak, b: (ulong*) b, c: (ulong*) c);
                DoPermutation(state2, state, CryptoStateLen, tablesForPermutations);
                tablesForPermutations += CryptoStateLen;

                DoThreefishForAllBlocks(state, state2, tweakTmp, tweakTmp2);
                DoPermutation(state2, state, CryptoStateLen, tablesForPermutations);
                tablesForPermutations += CryptoStateLen;

                // Вычисляем tweak для данного раунда (работаем со старшим 4-хбайтным словом младшего 8-мибайтного слова tweak)
                // Каждый раунд берёт +2 к старшему 4-хбайтовому слову; +1 - после первой половины, и +1 - после второй половины
                tweakTmp[0] += 0x1_0000_0000U;
            }

            // После последнего раунда производится рандомизация поблочной keccak-f
            DoKeccakForAllBlocks(state, CryptoStateLenKeccak, b: (ulong*) b, c: (ulong*) c);
            DoPermutation(state, state2, CryptoStateLen, transpose200_3200);
            DoKeccakForAllBlocks(state2, CryptoStateLenKeccak, b: (ulong*) b, c: (ulong*) c);
            DoPermutation(state2, state, CryptoStateLen, transpose200_3200);

            DoKeccakForAllBlocks(state, CryptoStateLenKeccak, b: (ulong*) b, c: (ulong*) c);
            DoPermutation(state, state2, CryptoStateLen, transpose200_3200);
            DoKeccakForAllBlocks(state2, CryptoStateLenKeccak, b: (ulong*) b, c: (ulong*) c);
            DoPermutation(state2, state, CryptoStateLen, transpose200_3200);
        }

        /// <summary>Выравнивает целое число i на интервал [0; ringModulo)</summary>
        /// <param name="i">Выравниваемое число</param>
        /// <param name="ringModulo">[0; ringModulo)</param>
        /// <returns>Выровненное число</returns>
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
        /// <param name="tweak">Базовый tweak для раунда. Не изменяется</param>
        /// <param name="len">Длина криптографического состояния в блоках ThreeFish1024 (по 128-мь байтов; ThreeFishBlockLen). len - нечётное</param>
        private static unsafe void DoThreefishForAllBlocks(byte* beginCryptoState, byte * finalCryptoState, ulong * tweak, ulong * tweakTmp)
        {
            int len = CryptoStateLenThreeFish;
            /*
            if ((len & 1) == 0)
                throw new ArgumentException("'len' must be odd", "len");
            */
            byte* cur = finalCryptoState;
            byte* key = beginCryptoState;

            BytesBuilder.CopyTo(CryptoStateLen, CryptoStateLen, beginCryptoState, finalCryptoState);

            tweakTmp[0] = tweak[0];
            tweakTmp[1] = tweak[1];

            // getNumberFromRing не вызывается, вместо этого используется самостоятельный расчёт, он должен быть более быстрым
            int j   = len >> 1;
            int add = 0;

            // cur - это финальное состояние, которое изменяется
            // key всегда вычисляется заново, т.к. он переходит через нуль - это массив ключевой информации для ThreeFish
            for (int i = 0; i < len; i++, j++, cur += ThreeFishBlockLen)
            {
                if (j >= len)
                    j = 0;

                add = j << 7; // blockLen * j;
                key = beginCryptoState + add;

                Threefish_Static_Generated.Threefish1024_step(key: (ulong *) key, tweak: (ulong *) tweakTmp, text: (ulong *) cur);

                tweakTmp[0] += 1;
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

        /// <summary>Осуществляет перестановки байтов для обеспечения диффузии</summary>
        /// <param name="source">Исходный массив: из него берутся значения</param>
        /// <param name="target">Целевой массив: в него записываются значения</param>
        /// <param name="len">Длины обоих массивов в байтах</param>
        /// <param name="permutationTable">Таблица перестановок</param>
        private static void DoPermutation(byte* source, byte* target, int len, ushort* permutationTable)
        {
            /*
             * Перестановка:
             * Теперь байт с позиции source[permutationTable[i]] мы переставляем на позицию target[i]
             * 
             * Например, transpose200 должна быть [0, 200, 400, 600, 800 ...]
             * 
             * */

             for (int i = 0; i < len; i++)
             {
                target[i] = source[permutationTable[i]];
             }
        }

        public static ushort[] transpose128_3200 = null;
        public static ushort[] transpose200_3200 = null;

        public static void GenTables()
        {
            if (transpose128_3200 != null)
                return;

            transpose128_3200 = GenTransposeTable(3200, 128);
            transpose200_3200 = GenTransposeTable(3200, 200);

            if (transpose128_3200[1] != 128)
                throw new Exception("VinKekFish: fatal algotirhmic error: GenTables - transpose128_3200[1] != 128");
            if (transpose128_3200[8] != 1024)
                throw new Exception("VinKekFish: fatal algotirhmic error: GenTables - transpose128_3200[8] != 1024");
            if (transpose128_3200[1] != 200)
                throw new Exception("VinKekFish: fatal algotirhmic error: GenTables - transpose128_3200[1] != 200");
            if (transpose128_3200[8] != 1600)
                throw new Exception("VinKekFish: fatal algotirhmic error: GenTables - transpose128_3200[8] != 1600");
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

            // Тестирование таблицы
            // Каждое значение должно быть представлено хотя бы один раз (и только один раз)
            for (ushort i = 0; i < newTable.Length; i++)
            {
                if (!newTable.Contains(i))
                {
                    throw new Exception("VinKekFish: fatal algotirhmic error 1: GenTransposeTable");
                }
            }

            var buffer1  = new byte[blockSize];
            var buffer2  = new byte[blockSize];
            // Двойное транспонирование 
            for (ushort i = 0; i < blockSize; i++)
            {
                buffer1[i] = (byte) i;
            }

            fixed (ushort* nt = newTable)
            fixed (byte*   b1 = buffer1, b2 = buffer2)
            {
                DoPermutation(b1, b2, blockSize, nt);
                DoPermutation(b2, b1, blockSize, nt);
            }

            for (ushort i = 0; i < blockSize; i++)
            {
                if (buffer1[i] != i)
                    throw new Exception("VinKekFish: fatal algotirhmic error 2: GenTransposeTable");
            }

            return newTable;
        }
    }
}
