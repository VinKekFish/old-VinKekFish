using cryptoprime.VinKekFish;
using cryptoprime;
using vinkekfish.keccak.keccak_20200918;

using static cryptoprime.VinKekFish.VinKekFishBase_etalonK1;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using static cryptoprime.BytesBuilderForPointers;

namespace vinkekfish
{
    /// <summary>В этом классе объявлена только инициализация и финализация: остальное в классах-потомках, реализующих конкретные схемы шифрования</summary>
    public unsafe class VinKekFish_k1_base_20210419: IDisposable
    {
        // Место на
        // Криптографическое состояние
        // Копию криптографического состояния
        // 4 tweak (основной и запасные)
        // new byte[CryptoStateLen * 2 + CryptoTweakLen * 4];
        // место для вспомогательных матриц c и b
        public readonly byte  [] state   = null;
        public          ushort[] pTables = null;
        public          int      RTables = 0;

        public          Record _state = null, _state2 = null, t0 = null, t1 = null, t2 = null, _transpose200_3200 = null, _b = null, _c = null;
        public          Record stateHandle   = null;
        public          Record pTablesHandle = null;

        protected bool isInited1 = false;
        protected bool isInited2 = false;

        public VinKekFish_k1_base_20210419()
        {
            GC.Collect();

            GenTables();
            GC.Collect();

            state = new byte[CryptoStateLen * 2 + CryptoTweakLen * 4 + cryptoprime.keccak.b_size + cryptoprime.keccak.c_size];
            BytesBuilder.ToNull(state);
        }

        /// <summary>Первичная инициализация: генерация таблиц перестановок (перед началом вызывает Clear)</summary>
        /// <param name="RoundsForTables">Количество раундов, под которое генерируются таблицы перестановок</param>
        /// <param name="additionalKeyForTables">Дополнительный ключ: это ключ для таблиц перестановок</param>
        /// <param name="OpenInitVectorForTables">Дополнительный вектор инициализации для перестановок (используется совместно с ключом)</param>
        /// <param name="PreRoundsForTranspose">Количество раундов со стандартными таблицами transpose< (не менее 1)/param>
        public virtual void Init1(int RoundsForTables, byte * additionalKeyForTables, long additionalKeyForTables_length, byte[] OpenInitVectorForTables = null, int PreRoundsForTranspose = 8)
        {
            Clear();
            GC.Collect();

            RTables = RoundsForTables;
            pTables = GenStandardPermutationTables(Rounds: RTables, key: additionalKeyForTables, key_length: additionalKeyForTables_length, OpenInitVector: OpenInitVectorForTables, PreRoundsForTranspose: PreRoundsForTranspose);
            pTablesHandle = new Fixed_AllocatorForUnsafeMemory().FixMemory(pTables);

            _transpose200_3200 = new Fixed_AllocatorForUnsafeMemory().FixMemory(transpose200_3200);

            GC.Collect();
            isInited1 = true;
        }

        /// <summary>Вторая инициализация: ввод ключа и ОВИ, обнуление состояния и т.п.</summary>
        /// <param name="key">Основной ключ</param>
        /// <param name="OpenInitVector">Основной вектор инициализации, может быть null</param>
        /// <param name="Rounds">Количество раундов при шифровании первого блока ключа (рекомендуется 16-64)</param>
        /// <param name="RoundsForEnd">Количество раундов при широфвании последующих блоков ключа (допустимо 4)</param>
        /// <param name="RoundsForExtendedKey">Количество раундов отбоя ключа (рекомендуется NORMAL_ROUNDS = 64)</param>
        public virtual void Init2(byte * key, ulong key_length, byte[] OpenInitVector, int Rounds = NORMAL_ROUNDS, int RoundsForEnd = NORMAL_ROUNDS, int RoundsForExtendedKey = REDUCED_ROUNDS)
        {
            if (!isInited1)
                throw new ArgumentOutOfRangeException("VinKekFish_k1_base_20210419: Init1 must be executed before Init2");

            // В этой и вызываемых функциях требуется проверка на наличие ошибок в неверных параметрах
            ClearState();
            if (pTables == null)
                throw new ArgumentOutOfRangeException("VinKekFish_k1_base_20210419: Init1 must be executed before Init2 (pTables == null)");

            // При изменении не забыть обнулить указатели в ClearState()
            stateHandle = new Fixed_AllocatorForUnsafeMemory().FixMemory(state);
            _state       = stateHandle.NoCopyClone(CryptoStateLen);
            _state2 = _state  + CryptoStateLen; // Это перегруженная операция сложения, _state2 идёт за массивом _state и имеет длину CryptoStateLen
            t0      = _state2 + CryptoTweakLen;
            t1      = t0      + CryptoTweakLen;
            t2      = t1      + CryptoTweakLen;
            _b      = t2      + cryptoprime.keccak.b_size;
            _c      = _b      + cryptoprime.keccak.c_size;

            fixed (byte * oiv = OpenInitVector)
            {
                InputKey
                (
                    key: key, key_length: key_length, OIV: oiv, OpenInitVector == null ? (ulong) OpenInitVector.LongLength : 0,
                    state: _state, state2: _state2, b: _b, c: _c,
                    tweak: t0, tweakTmp: t1, tweakTmp2: t2,
                    Initiated: false, SecondKey: false,
                    R: Rounds, RE: RoundsForEnd, RM: RoundsForExtendedKey, tablesForPermutations: pTablesHandle, transpose200_3200: _transpose200_3200
                );
            }

            isInited2 = true;
        }

        /// <summary>Очистка всех данных, включая таблицы перестановок. Использовать после окончания использования объекта (либо использовать Dispose)</summary>
        public void Clear()
        {
            isInited1 = false;
            ClearState();

            pTablesHandle?.Dispose();
            _transpose200_3200?.Dispose();

            RTables            = 0;
            pTablesHandle      = null;
            pTables            = null;
            _transpose200_3200 = null;

            Keccak_base_20200918.AllocPartOfMemory();
            GC.Collect();
        }

        /// <summary>Обнуляет состояние без перезаписи таблиц перестановок. Использовать после окончания шифрования, если нужно использовать объект повторно с другим ключом</summary>
        public void ClearState()
        {
            isInited2 = false;

            // Здесь обнуление состояния
            stateHandle?.Dispose();
            stateHandle = null;
            _state      = null;
            _state2     = null;
            t0          = null;
            t1          = null;
            t2          = null;
            t2          = null;
            t2          = null;
            _b          = null;
            _c          = null;
        }

        /// <summary>Генерирует стандартную таблицу перестановок</summary>
        /// <param name="Rounds">Количество раундов, для которых идёт генерация. Для каждого раунда по 4-ре таблицы</param>
        /// <param name="key">Это вспомогательный ключ для генерации таблиц перестановок. Основной ключ вводить нельзя! Этот ключ не может быть ключом, вводимым в VinKekFish, см. описание VinKekFish.md</param>
        /// <param name="PreRoundsForTranspose">Количество раундов, где таблицы перестановок не генерируются от ключа, а идут стандартно transpose128_3200 и transpose200_3200</param>
        public static ushort[] GenStandardPermutationTables(int Rounds, byte * key = null, long key_length = 0, byte[] OpenInitVector = null, int PreRoundsForTranspose = 8)
        {
            if (PreRoundsForTranspose < 1 || PreRoundsForTranspose > Rounds)
                throw new ArgumentOutOfRangeException("VinKekFish_base_20210419.GenStandardPermutationTables: PreRoundsForTranspose < 1 || PreRoundsForTranspose > Rounds");

            var prng = new Keccak_PRNG_20201128();

            if (key != null && key_length > 0)
                prng.InputKeyAndStep(key, key_length);

            if (OpenInitVector != null && OpenInitVector.Length > 0)
            {
                prng.InputBytes(OpenInitVector);
                prng.calcStep();
            }

            long len1  = VinKekFishBase_etalonK1.CryptoStateLen;
            long len2  = VinKekFishBase_etalonK1.CryptoStateLen << 1;

            var result = new ushort[len1 * Rounds * 4];
            var table1 = new ushort[len1];
            var table2 = new ushort[len1];

            for (ushort i = 0; i < table1.Length; i++)
            {
                table1[i] = i;
                table2[i] = (ushort) (table1.Length - i);
            }

            fixed (ushort * R = result)
            fixed (ushort * transpose200_3200_u = VinKekFishBase_etalonK1.transpose200_3200, transpose128_3200_u = VinKekFishBase_etalonK1.transpose128_3200)
            fixed (ushort * Table1 = table1, Table2 = table2)
            {
                ushort * r = R;
                byte * transpose200_3200 = (byte *) transpose200_3200_u;
                byte * transpose128_3200 = (byte *) transpose128_3200_u;

                for (; PreRoundsForTranspose > 0 && Rounds > 0; Rounds--, PreRoundsForTranspose--)
                {
                    BytesBuilder.CopyTo(len2, len2, transpose200_3200, (byte *) r); r += len1;
                    BytesBuilder.CopyTo(len2, len2, transpose128_3200, (byte *) r); r += len1;
                    BytesBuilder.CopyTo(len2, len2, transpose200_3200, (byte *) r); r += len1;
                    BytesBuilder.CopyTo(len2, len2, transpose128_3200, (byte *) r); r += len1;
                }

                prng.doRandomPermutationForUShorts(table1);
                prng.doRandomPermutationForUShorts(table2);

                for (; Rounds > 0; Rounds--)
                {
                    BytesBuilder.CopyTo(len2, len2, (byte *) Table1,   (byte *) r); r += len1;
                    BytesBuilder.CopyTo(len2, len2, (byte *) Table2,   (byte *) r); r += len1;
                    BytesBuilder.CopyTo(len2, len2, transpose200_3200, (byte *) r); r += len1;
                    BytesBuilder.CopyTo(len2, len2, transpose128_3200, (byte *) r); r += len1;
                }
            }

            return result;
        }

        public void Dispose()
        {
            Clear();
        }

        public void outputData(byte * output, long start, long outputLen, long countToOutput)
        {
            if (countToOutput > BLOCK_SIZE)
                throw new ArgumentOutOfRangeException("VinKekFish_k1_base_20210419.outputData: lenToOutput > BLOCK_SIZE");

            if (start + countToOutput > outputLen)
                throw new ArgumentOutOfRangeException("VinKekFish_k1_base_20210419.outputData: start + lenToOutput > len");

            BytesBuilder.CopyTo(countToOutput, outputLen, _state, output, start);
        }
    }
}
