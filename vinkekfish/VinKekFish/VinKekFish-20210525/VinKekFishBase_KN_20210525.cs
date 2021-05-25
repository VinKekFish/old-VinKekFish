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

namespace vinkekfish._20210525
{
    public unsafe partial class VinKekFishBase_KN_20210525: IDisposable
    {
        /// <summary>Здесь содержится два состояния, 4 твика на каждый блок TreeFish, матрицы c и b на каждый блок keccak. Матрицы c и b выровнены на 64 байта</summary>
        protected readonly Record   States = null;
        /// <summary>Здесь содержатся таблицы перестановок, длина CountOfRounds*4*Len*ushort</summary>
        protected readonly Record   tablesForPermutations = null;

        /// <summary>Аллокатор для выделения памяти внутри объекта</summary>
        public readonly BytesBuilderForPointers.AllocatorForUnsafeMemoryInterface allocator = new BytesBuilderForPointers.AllocHGlobal_AllocatorForUnsafeMemory();

                                                                            /// <summary>Криптографическое состояние 1</summary>
        public byte *  State1 => States;                                    /// <summary>Криптографическое состояние 2</summary>
        public byte *  State2 => States + Len;                              /// <summary>Массив матриц c и b на каждый блок Keccak</summary>
        public byte *  Matrix => State2 + Len;                              /// <summary>Массив tweak - по 4 tweak на каждый блок ThreeFish</summary>
        public ulong * Tweaks => (ulong *) (Matrix + MatrixArrayLen);                  
                                                                            /// <summary>Длина массива Tweaks в байтах</summary>
        public int TweaksArrayLen => 4 * CryptoTweakLen * LenInThreeFish;   /// <summary>Длина массива Matrix в байтах</summary>
        public int MatrixArrayLen => 256 * LenInKeccak;

                                                                            /// <summary>Максимальное количество раундов</summary>
        public readonly int CountOfRounds  = 0;                             /// <summary>Коэффициент размера K</summary>
        public readonly int K              = 1;              
                                                                            /// <summary>Размер криптографического состояния в байтах</summary>
        public readonly int Len            = 0;                             /// <summary>Размер криптографического состояния в блоках ThreeFish</summary>
        public readonly int LenInThreeFish = 0;                             /// <summary>Размер криптографического состояния в блока Keccak</summary>
        public readonly int LenInKeccak    = 0;

        protected readonly Thread[] threads = null;

        /// <summary>Создаёт и первично инициализирует объект VinKekFish (инициализация ключём и ОВИ должна быть отдельно). Создаёт Environment.ProcessorCount потоков для объекта</summary>
        /// <param name="CountOfRounds">Максимальное количество раундов шифрования, которое будет использовано, не менее VinKekFishBase_etalonK1.MIN_ROUNDS</param>
        /// <param name="K">Коэффициент размера K. Только нечётное число. Подробности смотреть в VinKekFish.md</param>
        /// <param name="PreRoundsForTranspose">Количество раундов без случайных таблиц перестановок (для инициализации таблиц перестановок)</param>
        /// <param name="keyForPermutations">Ключ для инициализации таблиц перестановок (не вводите сюда ключ шифрования для VinKekFish)</param>
        /// <param name="key_length">Длина ключа для таблиц перестановок</param>
        /// <param name="OpenInitVectorForPermutations">ОВИ (открытый вектор инициализации) для инициализации таблиц перестановок</param>
        /// <param name="OpenInitVectorForPermutations_length">Длина ОВИ</param>
        public VinKekFishBase_KN_20210525(int CountOfRounds = NORMAL_ROUNDS, int K = 1, int PreRoundsForTranspose = 8, byte * keyForPermutations = null, long key_length = 0, byte * OpenInitVectorForPermutations = null, long OpenInitVectorForPermutations_length = 0)
        {
            this.CountOfRounds = CountOfRounds;
            this.K             = K;

            if (CountOfRounds < MIN_ROUNDS)
                throw new ArgumentOutOfRangeException("VinKekFishBase_KN_20210525: CountOfRounds < MIN_ROUNDS");
            if (K < 1 || K > 19)
                throw new ArgumentOutOfRangeException("VinKekFishBase_KN_20210525: K < 1 || K > 19");
            if ((K & 1) == 0)
                throw new ArgumentOutOfRangeException("VinKekFishBase_KN_20210525: (K & 1) == 0. Read VinKekFish.md");

            Len            = K * CryptoStateLen;
            LenInThreeFish = Len / ThreeFishBlockLen;
            LenInKeccak    = Len / KeccakBlockLen;

            //                                   Состояния         Твики      c и b
            States         = allocator.AllocMemory(Len * 2 + TweaksArrayLen + MatrixArrayLen);
            ClearState();

            tablesForPermutations = VinKekFish_k1_base_20210419.GenStandardPermutationTables(CountOfRounds, allocator, key: keyForPermutations, key_length: key_length, OpenInitVector: OpenInitVectorForPermutations, OpenInitVector_length: OpenInitVectorForPermutations_length);


            threads = new Thread[Environment.ProcessorCount];
            for (int i = 0; i < threads.Length; i++)
                threads[i] = new Thread(ThreadsFunction);
        }

        /// <summary>Очистить всё состояние (кроме таблиц перестановок)</summary>
        public void ClearState()
        {
            BytesBuilder.ToNull(States, States);
        }

        /// <summary>Очистить вспомогательные массивы, включая второе состояние. Первичное состояние не очищается: объект остаётся инициализированным</summary>
        public void ClearSecondaryStates()
        {// TODO: В тестах проверить, что два шага без очистки равны двум шагам с очисткой между ними
            BytesBuilder.ToNull(targetLength: States - Len, States + Len);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected bool isDisposed =  false;
        public    bool IsDisposed => isDisposed;
        public virtual void Dispose(bool dispose)
        {
            isEnded = true;
            lock (sync)
                Monitor.PulseAll(sync);

            if (isDisposed)
                return;

            ClearState();
            States.Dispose();

            isDisposed = true;

            if (!dispose)
                throw new Exception("VinKekFishBase_KN_20210525.Dispose: you must call Dispose() after use");
        }

        ~VinKekFishBase_KN_20210525()
        {
            Dispose(false);
        }
    }
}
