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
    /// <summary>Основная реализация VinKekFish. Создаёт потоки внутри объекта для многопоточной обработки</summary>
    /// <remarks>IsDisposed == true означает, что объект более не пригоден для использования.</remarks>
    /// <remarks>Обязательная инициализация вызовом Init1 и Init2</remarks>
    public unsafe partial class VinKekFishBase_KN_20210525: IDisposable
    {
        /// <summary>Здесь содержится два состояния, 4 твика на каждый блок TreeFish, матрицы c и b на каждый блок keccak. Матрицы c и b выровнены на 64 байта</summary>
        protected readonly Record   States = null;
        /// <summary>Здесь содержатся таблицы перестановок, длина CountOfRounds*4*Len*ushort</summary>
        protected volatile Record   tablesForPermutations = null;

        /// <summary>Аллокатор для выделения памяти внутри объекта</summary>
        public readonly BytesBuilderForPointers.AllocatorForUnsafeMemoryInterface allocator = new BytesBuilderForPointers.AllocHGlobal_AllocatorForUnsafeMemory();

                                                                            /// <summary>Криптографическое состояние 1. Всегда в начале общего массива</summary>
        protected byte *  State1 => States;                                 /// <summary>Криптографическое состояние 2</summary>
        protected byte *  State2 => State1 + Len;
                                                                            /// <summary>Массив матриц b и c на каждый блок Keccak</summary>
        protected byte *  Matrix => State2 + Len;                           /// <summary>Массив tweak - по 4 tweak на каждый блок ThreeFish</summary>
        protected ulong * Tweaks => (ulong *) (Matrix + MatrixArrayLen);                  
                                                                            /// <summary>Длина массива Tweaks в байтах</summary>
        public readonly int TweaksArrayLen = 0;                             /// <summary>Длина массива Matrix в байтах</summary>
        public readonly int MatrixArrayLen = 0;                             /// <summary>Длина одного блока массива Matrix в байтах</summary>
        public const    int MatrixLen      = 256;

                                                                            /// <summary>Максимальное количество раундов</summary>
        public readonly int CountOfRounds  = 0;                             /// <summary>Коэффициент размера K</summary>
        public readonly int K              = 1;              
                                                                            /// <summary>Размер криптографического состояния в байтах</summary>
        public readonly int Len            = 0;                             /// <summary>Размер криптографического состояния в блоках ThreeFish</summary>
        public readonly int LenInThreeFish = 0;                             /// <summary>Размер криптографического состояния в блока Keccak</summary>
        public readonly int LenInKeccak    = 0;
                                                                            /// <summary>Размер криптографического состояния, поделенного между потоками, в байтах. Последний блок может быть большей длины</summary>
        public readonly int LenThreadBlock   = 0;                           /// <summary>Количество блоков перестановки для потоков (размер в блоках длиной LenThreadBlock)</summary>
        public readonly int LenInThreadBlock = 0;                           /// <summary>Количество потоков</summary>
        public readonly int ThreadCount      = 0;

        /// <summary>Вспомогательные переменные, показывающие, какие состояния сейчас являются целевыми. Изменяются в алгоритме</summary>
        protected volatile byte * st1 = null, st2 = null, st3 = null;
        /// <summary>Массив, устанавливающий номера ключевых блоков TreeFish для каждого трансформируемого блока</summary>
        protected readonly int[] NumbersOfThreeFishBlocks = null;
        protected readonly Timer Timer                    = null;

        /// <summary>Создаёт и первично инициализирует объект VinKekFish (инициализация ключём и ОВИ должна быть отдельно). Создаёт Environment.ProcessorCount потоков для объекта</summary>
        /// <param name="CountOfRounds">Максимальное количество раундов шифрования, которое будет использовано, не менее VinKekFishBase_etalonK1.MIN_ROUNDS</param>
        /// <param name="K">Коэффициент размера K. Только нечётное число. Подробности смотреть в VinKekFish.md</param>
        /// <param name="PreRoundsForTranspose">Количество раундов без случайных таблиц перестановок (для инициализации таблиц перестановок)</param>
        /// <param name="keyForPermutations">Ключ для инициализации таблиц перестановок (не вводите сюда ключ шифрования для VinKekFish)</param>
        /// <param name="key_length">Длина ключа для таблиц перестановок</param>
        /// <param name="OpenInitVectorForPermutations">ОВИ (открытый вектор инициализации) для инициализации таблиц перестановок</param>
        /// <param name="OpenInitVectorForPermutations_length">Длина ОВИ</param>
        /// <param name="ThreadCount">Количество создаваемых потоков. Рекомендуется использовать значение по-умолчанию: 0 (0 == Environment.ProcessorCount)</param>
        public VinKekFishBase_KN_20210525(int CountOfRounds = NORMAL_ROUNDS, int K = 1, int ThreadCount = 0, int TimerIntervalMs = 1000)
        {
            TweaksArrayLen = 4 * CryptoTweakLen * LenInThreeFish;
            MatrixArrayLen = MatrixLen * LenInKeccak;

            if (ThreadCount <= 0)
                ThreadCount = Environment.ProcessorCount;

            this.ThreadCount   = ThreadCount;
            this.ThreadsInFunc = ThreadCount;
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

            // Вообще говоря, больше 2-х потоков на перестановке может быть не оправдано, однако там всё сложно
            LenInThreadBlock = ThreadCount;
            LenThreadBlock   = Len / LenInThreadBlock;
            if (LenThreadBlock < 512 || Len % LenInThreadBlock > 0)
            {
                if (LenThreadBlock < 512)
                {
                    LenThreadBlock   = 512;
                    LenInThreadBlock = Len / LenThreadBlock;
                }

                // Пытаемся увеличить количество блоков так, чтобы минимальный блок был хотя бы 256, и размер был всегда кратный линии кеша и кратный длине состояния
                while (Len % LenInThreadBlock > 0 || LenThreadBlock % 64 > 0)
                {
                    if (LenThreadBlock <= 256)
                        break;

                    LenInThreadBlock++;
                    LenThreadBlock = Len / LenInThreadBlock;
                }

                if (Len % LenInThreadBlock > 0 || LenThreadBlock % 64 > 0)
                    throw new Exception("VinKekFishBase_KN_20210525: Fatal algorithmic error");
            }

            //                            Состояния      Твики            b и c
            States = allocator.AllocMemory(Len * 2 + TweaksArrayLen + MatrixArrayLen);
            ClearState();

            ThreadsFunc_Current = ThreadFunction_empty;
            threads = new Thread[ThreadCount];

            for (int i = 0; i < threads.Length; i++)
                threads[i] = new Thread(ThreadsFunction);


            NumbersOfThreeFishBlocks = new int[LenInThreeFish];
            var j = LenInThreeFish / 2;
            for (int i = 0; i < LenInThreeFish; i++)
            {
                NumbersOfThreeFishBlocks[i] = j++;
                if (j >= LenInThreeFish)
                    j = 0;
            }

            CheckNumbersOfThreeFishBlocks();

            if (TimerIntervalMs > 0)
            {
                Timer = new Timer(WaitFunction, period: TimerIntervalMs, dueTime: TimerIntervalMs, state: this);
            }
        }

        /// <summary>Проверка верности заполнения NumbersOfThreeFishBlocks</summary>
        protected void CheckNumbersOfThreeFishBlocks()
        {
            var nums = new int[LenInThreeFish];
            for (int i = 0; i < LenInThreeFish; i++)
                nums[i] = -1;

            int j = 0;
            for (int i = 0; i < LenInThreeFish; i++)
            {
                var k = NumbersOfThreeFishBlocks[j];
                if (nums[j] >= 0)
                    throw new Exception("VinKekFishBase_KN_20210525.CheckNumbersOfThreeFishBlocks: Fatal algorithmic error");

                nums[j] = k;
                j = k;
            }
        }

        public virtual void Init1(int PreRoundsForTranspose = 8, byte * keyForPermutations = null, long key_length = 0, byte * OpenInitVectorForPermutations = null, long OpenInitVectorForPermutations_length = 0)
        {
            tablesForPermutations = VinKekFish_k1_base_20210419.GenStandardPermutationTables(CountOfRounds, allocator, key: keyForPermutations, key_length: key_length, OpenInitVector: OpenInitVectorForPermutations, OpenInitVector_length: OpenInitVectorForPermutations_length);
        }

        /// <summary>Очистить всё состояние (кроме таблиц перестановок)</summary>
        public virtual void ClearState()
        {
            ThreadsFunc_Current = ThreadFunction_empty;
            BytesBuilder.ToNull(States, States);
        }

        /// <summary>Очистить вспомогательные массивы, включая второе состояние. Первичное состояние не очищается: объект остаётся инициализированным</summary>
        public virtual void ClearSecondaryStates()
        {// TODO: В тестах проверить, что два шага без очистки равны двум шагам с очисткой между ними
            BytesBuilder.ToNull(targetLength: States - Len, States + Len);
        }

        public virtual void ClearPermutationTables()
        {
            lock (this)
            {
                tablesForPermutations?.Dispose();
                tablesForPermutations = null;
            }
        }

        public virtual void Clear()
        {
            ClearState();
            ClearPermutationTables();
        }

        /// <summary>Очищает объект и освобождает все выделенные под него ресурсы</summary>
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

            Clear();
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
