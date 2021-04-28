using cryptoprime.VinKekFish;
using cryptoprime;
using vinkekfish.keccak.keccak_20200918;

using static cryptoprime.VinKekFish.VinKekFishBase_etalonK1;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace vinkekfish
{
    /// <summary>В этом классе объявлена только инициализация и финализация: остальное в классах-потомках, реализующих конкретные схемы шифрования</summary>
    public unsafe class VinKekFish_k1_base_20210419: IDisposable
    {
        // Выделяем сразу место на
        // Криптографическое состояние
        // Копию криптографического состояния
        // 4 tweak (основной и запасные)
        // new byte[CryptoStateLen * 2 + CryptoTweakLen * 4];
        public readonly byte  [] state   = null;
        public          ushort[] pTables = null;

        public VinKekFish_k1_base_20210419()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();

            GenTables();
            GC.Collect();

            state = new byte[CryptoStateLen * 2 + CryptoTweakLen * 4];
            BytesBuilder.ToNull(state);
        }

        /// <summary>Первичная инициализация: генерация таблиц перестановок (перед началом вызывает Clear)</summary>
        /// <param name="RoundsForTables">Количество раундов, под которое генерируются таблицы перестановок</param>
        /// <param name="additionalKeyForTables">Дополнительный ключ: это ключ для таблиц перестановок</param>
        /// <param name="OpenInitVectorForTables">Дополнительный вектор инициализации для перестановок (используется совместно с ключом)</param>
        /// <param name="PreRoundsForTranspose">Количество раундов со стандартными таблицами transpose< (не менее 1)/param>
        public void Init1(int RoundsForTables, byte[] additionalKeyForTables, byte[] OpenInitVectorForTables = null, int PreRoundsForTranspose = 8)
        {
            Clear();
            GC.Collect();

            pTables = GenStandardPermutationTables(Rounds: RoundsForTables, key: additionalKeyForTables, OpenInitVector: OpenInitVectorForTables, PreRoundsForTranspose: PreRoundsForTranspose);
            GC.Collect();
        }

        /// <summary>Вторая инициализация: ввод ключа и ОВИ, обнуление состояния и т.п.</summary>
        /// <param name="key">Основной ключ</param>
        /// <param name="OpenInitVector">Основной вектор инициализации, может быть null</param>
        /// <param name="Rounds">Количество раундов при шифровании первого блока ключа (рекомендуется 16-64)</param>
        /// <param name="RoundsForEnd">Количество раундов при широфвании последующих блоков ключа</param>
        /// <param name="RoundsForExtendedKey">Количество раундов отбоя ключа (рекомендуется 64)</param>
        public void Init2(byte[] key, byte[] OpenInitVector, ulong Rounds = 64, ulong RoundsForEnd = 64, ulong RoundsForExtendedKey = 4)
        {
            // В этой и вызываемых функциях требуется проверка на наличие ошибок в неверных параметрах
            ClearState();
            if (pTables == null)
                throw new ArgumentOutOfRangeException("VinKekFish_k1_base_20210419: Init1 must be executed before Init2");

            fixed (byte   * k  = key, oiv = OpenInitVector, st1 = state)
            fixed (ushort * pt = pTables, tr = transpose200_3200)
            {
                byte * b = stackalloc byte[25*8];
                byte * c = stackalloc byte[ 5*8];
                byte * t = st1 + CryptoStateLen * 2;

                InputKey
                (
                    key: k, key_length: (ulong) key.LongLength, OIV: oiv, OpenInitVector == null ? (ulong) OpenInitVector.LongLength : 0,
                    state: st1, state2: st1 + CryptoStateLen, b: b, c: c,
                    tweak: (ulong *) t, tweakTmp: (ulong *) (t + CryptoTweakLen * 1), tweakTmp2: (ulong *) (t + CryptoTweakLen * 2),
                    Initiated: false, SecondKey: false,
                    R: Rounds, RE: RoundsForEnd, RM: RoundsForExtendedKey, tablesForPermutations: pt, transpose200_3200: tr
                );

                BytesBuilder.ToNull(25*8, b);
                BytesBuilder.ToNull( 5*8, c);
            }
        }

        /// <summary>Очистка всех данных, включая таблицы перестановок. Использовать после окончания использования объекта (либо использовать Dispose)</summary>
        public void Clear()
        {
            ClearState();

            if (pTables != null)
            {
                fixed (ushort * pt = pTables)
                {
                    byte * p = (byte *) pt;
                    BytesBuilder.ToNull(pTables.LongLength << 1, p);
                }

                pTables = null;
            }
        }

        /// <summary>Обнуляет состояние без перезаписи таблиц перестановок. Использовать после окончания шифрования, если нужно использовать объект повторно с другим ключом</summary>
        public void ClearState()
        {
            BytesBuilder.ToNull(state);
        }

        /// <summary>Генерирует стандартную таблицу перестановок</summary>
        /// <param name="Rounds">Количество раундов, для которых идёт генерация. Для каждого раунда по 4-ре таблицы</param>
        /// <param name="key">Это вспомогательный ключ для генерации таблиц перестановок. Основной ключ вводить нельзя! Этот ключ не может быть ключом, вводимым в VinKekFish, см. описание VinKekFish.md</param>
        /// <param name="PreRoundsForTranspose">Количество раундов, где таблицы перестановок не генерируются от ключа, а идут стандартно transpose128_3200 и transpose200_3200</param>
        public static ushort[] GenStandardPermutationTables(int Rounds, byte[] key = null, byte[] OpenInitVector = null, int PreRoundsForTranspose = 8)
        {
            if (PreRoundsForTranspose < 1 || PreRoundsForTranspose > Rounds)
                throw new ArgumentOutOfRangeException("VinKekFish_base_20210419.GenStandardPermutationTables: PreRoundsForTranspose < 1 || PreRoundsForTranspose > Rounds");

            var prng = new Keccak_PRNG_20201128();
            
            if (key != null && key.Length > 0)
                prng.InputKeyAndStep(key);

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
    }
}
