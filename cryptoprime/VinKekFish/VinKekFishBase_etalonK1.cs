using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeGenerated.Cryptoprimes;

namespace cryptoprime.VinKekFish
{
    /// <summary>Базовая однопоточная реализация VinKekFish для K = 1. Использование для тестирования. См. также descr.md</summary>
    public static unsafe class VinKekFishBase_etalonK1
    {
        // Размер криптографического состояния
        public const int CryptoStateLen          = 3200; // В байтах
        public const int CryptoStateLenKeccak    = CryptoStateLen / KeccakBlockLen;    // Размер криптографического состояния в блоках keccak
        public const int CryptoStateLenThreeFish = CryptoStateLen / ThreeFishBlockLen; // Размер криптографического состояния в блоках ThreeFish

        public const int BLOCK_SIZE              = 512;
        public const int MAX_SINGLE_KEY          = 2048;
        public const int MAX_OIV                 = 1148;
        public const int MIN_ROUNDS              = 4;

        // b.len = 25*8, c.len = 5*8
        // var prt = System.Runtime.InteropServices.Marshal.AllocHGlobal(len) and FreeHGlobal либо stackalloc
        public static void InputKey(byte * key, ulong key_length, byte * OIV, ulong OIV_length, byte * state, byte * state2, byte * b, byte *c, ulong * tweak, ulong * tweakTmp, ulong * tweakTmp2, bool Initiated, bool SecondKey, ulong R, ulong RE, ulong RM, ushort * tablesForPermutations, ushort * transpose200_3200)
        {
            if (SecondKey && OIV != null)
                throw new ArgumentException("VinKekFishBase_etalonK1.InputKey: SecondKey && OIV != null");

            if (SecondKey && RE != 0)
                throw new ArgumentOutOfRangeException("SecondKey && RE != 0");

            if (SecondKey != Initiated)
                throw new ArgumentOutOfRangeException("SecondKey != Initiated");

            if (OIV == null && OIV_length != 0)
                throw new ArgumentOutOfRangeException("VinKekFishBase_etalonK1.InputKey: OIV == null && OIV_length != 0");

            if (OIV != null && OIV_length > MAX_OIV)
                throw new ArgumentOutOfRangeException("VinKekFishBase_etalonK1.InputKey: OIV_length > MAX_OIV");

            if (key == null)
                throw new ArgumentNullException("VinKekFishBase_etalonK1.InputKey: key == null");

            if (key_length == 0)
                throw new ArgumentNullException("VinKekFishBase_etalonK1.InputKey: key_length == 0");

            if (R < MIN_ROUNDS)
                throw new ArgumentOutOfRangeException("R < MIN_ROUNDS");

            var dataLen = key_length;
            var data    = key;
            if (SecondKey)
            {
                if (dataLen > BLOCK_SIZE)
                    dataLen = BLOCK_SIZE;

                for (ulong i = 0; i < dataLen; i++, data++)
                {
                    state[i+2] ^= *data;
                }
            }
            else
            {
                if (dataLen > MAX_SINGLE_KEY)
                    dataLen = MAX_SINGLE_KEY;

                for (ulong i = 0; i < dataLen; i++, data++)
                {
                    state[i+2] = *data;
                }
            }

            byte len1 = (byte) dataLen;
            byte len2 = (byte) (dataLen >> 8);

            state[0] ^= len1;
            state[1] ^= len2;

            tweak[0] += 1253539379;

            if (!SecondKey)
            {
                tweak[1] += key_length;

                if (OIV != null && OIV_length > 0)
                {
                    len1 = (byte) OIV_length;
                    len2 = (byte) (OIV_length >> 8);

                    state[2050] ^= len1;
                    state[2051] ^= len2;

                    for (ulong i = 0; i < OIV_length; i++, OIV++)
                    {
                        state[i+2052] = *OIV;
                    }
                }
            }

            // TODO: указатели на таблицы перестановок
            step
            (
                countOfRounds: R, tablesForPermutations: tablesForPermutations, transpose200_3200: transpose200_3200,
                tweak: tweak, tweakTmp: tweakTmp, tweakTmp2: tweakTmp2, state: state, state2: state, b: b, c: c
            );

            if (key_length > dataLen)
            {
                InputKey
                (
                    key:        data,
                    key_length: key_length - dataLen,

                    SecondKey:  true,
                    Initiated:  true,

                    OIV:        null,
                    OIV_length: 0,

                    R:          RM,             // Повторный ввод ключа осуществляется под RM раундов
                    RM:         RM,
                    RE:         0,

                    state: state, state2: state2, tweak: tweak, tweakTmp: tweakTmp, tweakTmp2: tweakTmp2, b: b, c: c, tablesForPermutations: tablesForPermutations, transpose200_3200: transpose200_3200
                );
            }

            // Завершаем ввод ключа отбоем. Т.к. вызов функции рекурсивный, отбой происходит только в самой верхней функции - SecondKey = false
            if (!SecondKey)
            {
                InputData_Overwrite(data: null, state: state, dataLen: 0, tweak: tweak, regime: 255);
                step
                (
                    countOfRounds: RE, tablesForPermutations: tablesForPermutations, transpose200_3200: transpose200_3200,
                    tweak: tweak, tweakTmp: tweakTmp, tweakTmp2: tweakTmp2, state: state, state2: state, b: b, c: c
                );
            }
        }

        /// <summary>Сырой ввод данных. Вводит данные в состояние путём перезатирания (режим OVERWRITE), изменяет tweak</summary>
        /// <param name="data">Указатель на вводимые данные</param>
        /// <param name="state">Указатель на криптографическое состояние</param>
        /// <param name="dataLen">Длина вводимых данных, не более BLOCK_SIZE</param>
        /// <param name="tweak">Указатель на tweak (для соответствующего изменения tweak)</param>
        /// <param name="regime">Счётчик режима ввода</param>
        public static void InputData_Overwrite(byte * data, byte * state, ulong dataLen, ulong * tweak, byte regime)
        {
            if (dataLen > BLOCK_SIZE)
                throw new ArgumentOutOfRangeException();

            ulong i = 0;
            for (; i < dataLen; i++, data++)
            {
                state[i+3] = *data;
            }

            for (; i < BLOCK_SIZE; i++)
            {
                state[i+3] = 0;
            }

            byte len1 = (byte) dataLen;
            byte len2 = (byte) (dataLen >> 8);

            len2 &= 0x80;   // Старший бит количества вводимых байтов устанавливается в 1, если используется режим Overwrite

            state[0] ^= len1;
            state[1] ^= len2;
            state[2] ^= regime;

            InputData_ChangeTweak(tweak: tweak, dataLen: dataLen, Overwrite: true, regime: regime);
        }

        /// <summary>Сырой ввод данных. Вводит данные в состояние через xor (режим ввода sponge)</summary>
        public static void InputData_Xor(byte * data, byte * state, ulong dataLen, ulong * tweak, byte regime)
        {
            if (dataLen > BLOCK_SIZE)
                throw new ArgumentOutOfRangeException();

            for (ulong i = 0; i < dataLen; i++, data++)
            {
                state[i+3] ^= *data;
            }

            byte len1 = (byte) dataLen;
            byte len2 = (byte) (dataLen >> 8);

            state[0] ^= len1;
            state[1] ^= len2;
            state[2] ^= regime;

            InputData_ChangeTweak(tweak: tweak, dataLen: dataLen, Overwrite: false, regime: regime);
        }

        /// <summary>Этот метод вызывать не надо. Он автоматически вызывается при вызове InputData_*</summary>
        public static void InputData_ChangeTweak(ulong * tweak, ulong dataLen, bool Overwrite, byte regime)
        {
            // Приращение tweak перед вводом данных
            tweak[0] += 1253539379;

            tweak[1] += dataLen;
            if (Overwrite)
                tweak[1] += 0x0100_0000_0000_0000;

            var reg = ((ulong) regime) << 40; // 8*5 - третий по старшинству байт, нумерация с 1
            tweak[1] += reg;
        }

        /// <summary>Шаг алгоритма ПОСЛЕ ввода данных</summary>
        /// <param name="countOfRounds">Количество раундов</param>
        /// <param name="tweak">Tweak после ввода данных, 16 байтов (все массивы могут быть в одном, если это удобно). Не изменяется в функции.</param>
        /// <param name="tweakTmp">Дополнительный массив для временного tweak, 16 байтов. Изменяется в функции.</param>
        /// <param name="tweakTmp2">Дополнительный массив для временного tweak, 16 байтов. Изменяется в функции.</param>
        /// <param name="state">Криптографическое состояние</param>
        /// <param name="state2">Вспомогательный массив для криптографического состояния</param>
        /// <param name="tablesForPermutations">Массив таблиц перестановок на каждый раунд. Длина должна быть countOfRounds*4 (*CryptoStateLen*ushort на каждую таблицу)</param>
        /// <param name="b">Вспомогательный массив b для keccak.Keccackf</param>
        /// <param name="c">Вспомогательный массив c для keccak.Keccackf</param>
        public static void step(ulong countOfRounds, ulong * tweak, ulong * tweakTmp, ulong * tweakTmp2, byte * state, byte * state2, ushort * tablesForPermutations, byte* b, byte* c, ushort * transpose200_3200)
        {
            DoPermutation(state, state2, CryptoStateLen, transpose200_3200);
            BytesBuilder.CopyTo(CryptoStateLen, CryptoStateLen, state2, state);

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

            // После последнего раунда производится рандомизация поблочной функцией keccak-f
            for (int i = 0; i < 2; i++)
            {
                DoKeccakForAllBlocks(state, CryptoStateLenKeccak, b: (ulong*) b, c: (ulong*) c);
                DoPermutation(state, state2, CryptoStateLen, transpose200_3200);
                DoKeccakForAllBlocks(state2, CryptoStateLenKeccak, b: (ulong*) b, c: (ulong*) c);
                DoPermutation(state2, state, CryptoStateLen, transpose200_3200);
            }
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
        public static unsafe void DoThreefishForAllBlocks(byte* beginCryptoState, byte * finalCryptoState, ulong * tweak, ulong * tweakTmp)
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
        public static unsafe void DoKeccakForAllBlocks(byte* CryptoState, int len, ulong * b, ulong * c)
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
        public static void DoPermutation(byte* source, byte* target, int len, ushort* permutationTable)
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

        public static readonly object sync = new object();

        /// <summary>Эту процедуру нужно вызвать для инициализации таблиц перестановок перед любым вызовом методов класса. Допускается многопоточный вызов без синхронизации. Вызов производится один раз на всю программу (на весь процесс)</summary>
        public static void GenTables()
        {
            lock (sync)
            {
                if (transpose128_3200 != null)
                    return;

                transpose128_3200 = GenTransposeTable(3200, 128);
                transpose200_3200 = GenTransposeTable(3200, 200);
            }

            if (transpose128_3200[1] != 128)
                throw new Exception("VinKekFish: fatal algotirhmic error: GenTables - transpose128_3200[1] != 128");
            if (transpose128_3200[8] != 1024)
                throw new Exception("VinKekFish: fatal algotirhmic error: GenTables - transpose128_3200[8] != 1024");
            if (transpose200_3200[1] != 200)
                throw new Exception("VinKekFish: fatal algotirhmic error: GenTables - transpose200_3200[1] != 200");
            if (transpose200_3200[8] != 1600)
                throw new Exception("VinKekFish: fatal algotirhmic error: GenTables - transpose200_3200[8] != 1600");
            if (transpose200_3200[400] != 25)
                throw new Exception("VinKekFish: fatal algotirhmic error: GenTables - transpose200_3200[400] != 25");
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
            /*
            var buffer1  = new byte[blockSize];
            var buffer2  = new byte[blockSize];
            // Двойное транспонирование: эта штука не работает. Здесь транспонирование не является операцией, которая обратна самой себе
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
            buffer1 = null;
            buffer2 = null;
            */
            return newTable;
        }
    }
}
