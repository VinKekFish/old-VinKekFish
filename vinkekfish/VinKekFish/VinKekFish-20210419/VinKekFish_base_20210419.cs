using cryptoprime.VinKekFish;
using cryptoprime;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using vinkekfish.keccak.keccak_20200918;

namespace vinkekfish
{
    public unsafe class VinKekFish_base_20210419
    {
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

                for (; PreRoundsForTranspose > 0; Rounds--, PreRoundsForTranspose--)
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

    }
}
