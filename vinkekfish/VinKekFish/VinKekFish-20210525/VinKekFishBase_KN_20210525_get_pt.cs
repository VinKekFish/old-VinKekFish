using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using cryptoprime;
using cryptoprime.VinKekFish;
using vinkekfish.keccak_20200918;
using static cryptoprime.BytesBuilderForPointers;
using static cryptoprime.VinKekFish.VinKekFishBase_etalonK1;

namespace vinkekfish
{
    public unsafe partial class VinKekFishBase_KN_20210525: IDisposable
    {
        
        /// <summary>Генерирует стандартную таблицу перестановок</summary>
        /// <param name="Rounds">Количество раундов, для которых идёт генерация. Для каждого раунда по 4-ре таблицы</param>
        /// <param name="key">Это вспомогательный ключ для генерации таблиц перестановок. Основной ключ вводить нельзя! Этот ключ не может быть ключом, вводимым в VinKekFish, см. описание VinKekFish.md</param>
        /// <param name="PreRoundsForTranspose">Количество раундов, где таблицы перестановок не генерируются от ключа, а идут стандартно transpose128_3200 и transpose200_3200</param>
        public static Record GenStandardPermutationTables(int Rounds, AllocatorForUnsafeMemoryInterface allocator = null, byte * key = null, long key_length = 0, byte * OpenInitVector = null, long OpenInitVector_length = 0, int PreRoundsForTranspose = 8)
        {
            GenTables();

            if (PreRoundsForTranspose < 1 || PreRoundsForTranspose > Rounds)
                throw new ArgumentOutOfRangeException("VinKekFish_base_20210419.GenStandardPermutationTables: PreRoundsForTranspose < 1 || PreRoundsForTranspose > Rounds");

            if (allocator == null)
                allocator = VinKekFish_k1_base_20210419.AllocHGlobal_allocator;

            using var prng = new Keccak_PRNG_20201128();

            if (key != null && key_length > 0)
            {
                if (OpenInitVector == null)
                    prng.InputKeyAndStep(key, key_length, null, 0);
                else
                {
                    prng.InputKeyAndStep(key, key_length, OpenInitVector, OpenInitVector_length);
                }
            }
            else
            if (OpenInitVector != null)
                throw new ArgumentException("key == null && OpenInitVector != null. Set OpenInitVector as key");

            long len1  = VinKekFishBase_etalonK1.CryptoStateLen;
            long len2  = VinKekFishBase_etalonK1.CryptoStateLen << 1;

            var result = allocator.AllocMemory(Rounds * 4 * len1 * sizeof(ushort));
            var table1 = new ushort[len1];
            var table2 = new ushort[len1];

            for (ushort i = 0; i < table1.Length; i++)
            {
                table1[i] = i;
                table2[i] = (ushort) (table1.Length - i - 1);
            }

            fixed (ushort * Table1 = table1, Table2 = table2)
            {
                ushort * R = result;
                ushort * r = R;

                for (; PreRoundsForTranspose > 0 && Rounds > 0; Rounds--, PreRoundsForTranspose--)
                {
                    BytesBuilder.CopyTo(len2, len2, (byte *) transpose200_3200_8, (byte *) r); r += len1;
                    BytesBuilder.CopyTo(len2, len2, (byte *) transpose128_3200  , (byte *) r); r += len1;
                    BytesBuilder.CopyTo(len2, len2, (byte *) transpose200_3200  , (byte *) r); r += len1;
                    BytesBuilder.CopyTo(len2, len2, (byte *) transpose128_3200  , (byte *) r); r += len1;
                }
// TODO: Сколько можно ввести дополнительной рандомизирующей информации, чтобы она вводилась при перестановках от раунда к раунду
                for (; Rounds > 0; Rounds--)
                {
                    prng.doRandomPermutationForUShorts(table1);
                    prng.doRandomPermutationForUShorts(table2);
/*  // Если необходимо, раскомментировать отладочный код: здесь проверяется, что перестановки были корректны (что они перестановки, а не какие-то ошибки)
#if DEBUG
                    CheckPermutationTable(table1);
                    CheckPermutationTable(table2);
#endif
*/
                    BytesBuilder.CopyTo(len2, len2, (byte*)Table1,              (byte*)r); r += len1;
                    BytesBuilder.CopyTo(len2, len2, (byte*)Table2,              (byte*)r); r += len1;
                    BytesBuilder.CopyTo(len2, len2, (byte*)transpose200_3200  , (byte*)r); r += len1;
                    BytesBuilder.CopyTo(len2, len2, (byte*)transpose128_3200  , (byte*)r); r += len1;
                }

                BytesBuilder.ToNull(table1.Length * sizeof(ushort), (byte *) Table1);
                BytesBuilder.ToNull(table1.Length * sizeof(ushort), (byte *) Table2);
            }

            return result;
        }

#if DEBUG
        private static void CheckPermutationTable(ushort[] table1)
        {
            bool found;
            for (int i = 0; i < table1.Length; i++)
            {
                found = false;
                for (int j = 0; j < table1.Length; j++)
                {
                    if (table1[j] == i)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                    throw new Exception($"DEBUG: doRandomPermutationForUShorts incorrect: value {i} not found");
            }
        }
#endif

    }
}
