using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using cryptoprime;
using cryptoprime.VinKekFish;

using static cryptoprime.BytesBuilderForPointers;
using static cryptoprime.VinKekFish.VinKekFishBase_etalonK1;

namespace vinkekfish
{
    public unsafe partial class VinKekFishBase_KN_20210525: IDisposable
    {
        protected bool isInit1 = false;
        protected bool isInit2 = false;

        /// <summary>Осуществляет непосредственный шаг алгоритма без ввода данных и изменения tweak</summary><remarks>Вызывайте эту функцию, если хотите переопределить поведение VinKekFish</remarks>
        /// <param name="countOfRounds">Количество раундов. См. </param>
        public void step(int countOfRounds = -1)
        {
            if (!isInit1)
                throw new Exception("VinKekFishBase_KN_20210525.step: you must call Init1 before doing this");

            if (countOfRounds < 0)
                countOfRounds = this.CountOfRounds;

            var TB = tablesForPermutations;
            State1Main = true;

            // Предварительное преобразование
            doPermutation(transpose128_3200);
            doThreeFish();
            doPermutation(transpose128_3200);

            BytesBuilder.CopyTo(CryptoStateLen, CryptoStateLen, State2, State1); State1Main = true;

            // Основной шаг алгоритма: раунды
            // Каждая итерация цикла - это полураунд
            countOfRounds <<= 1;
            for (int round = 0; round < countOfRounds; round++)
            {
                doKeccak();
                doPermutation(TB); TB += CryptoStateLen;

                doThreeFish();
                doPermutation(TB); TB += CryptoStateLen;

                // Довычисление tweakVal для второго преобразования VinKekFish
                // Вычисляем tweak для данного раунда (работаем со старшим 4-хбайтным словом младшего 8-мибайтного слова tweak)
                // Каждый раунд берёт +2 к старшему 4-хбайтовому слову; +1 - после первого полураунда, и +1 - после второго полураунда
                Tweaks[2+0] += 0x1_0000_0000U;  // Берём элемент [1], расположение tweak см. по метке :an6c5JhGzyOO
            }

            // После последнего раунда производится заключительное преобразование (заключительная рандомизация) поблочной функцией keccak-f
            for (int i = 0; i < CountOfFinal; i++)
            {
                doKeccak();
                doPermutation(transpose200_3200);
                doKeccak();
                doPermutation(transpose200_3200_8);
            }

            if (!State1Main)
                throw new Exception("VinKekFishBase_KN_20210525.step: Fatal algorithmic error: !State1Main");
        }

        /// <summary>Предварительная инициализация объекта. Осуществляет установку таблиц перестановок.</summary>
        /// <param name="PreRoundsForTranspose">Количество раундов, которое будет происходить со стандартными таблицами (не зависящими от ключа)</param>
        /// <param name="keyForPermutations">Дополнительный ключ: ключ для определения таблиц перестановок</param>
        /// <param name="key_length">Длина ключа</param>
        /// <param name="OpenInitVectorForPermutations">Дополнительный вектор инициализации</param>
        /// <param name="OpenInitVectorForPermutations_length">Длина дополнительного вектора инициализации</param>
        public virtual void Init1(int PreRoundsForTranspose = 8, byte * keyForPermutations = null, long key_length = 0, byte * OpenInitVectorForPermutations = null, long OpenInitVectorForPermutations_length = 0)
        {
            tablesForPermutations = VinKekFish_k1_base_20210419.GenStandardPermutationTables(CountOfRounds, allocator, key: keyForPermutations, key_length: key_length, OpenInitVector: OpenInitVectorForPermutations, OpenInitVector_length: OpenInitVectorForPermutations_length);
            isInit1 = true;
        }
    }
}
