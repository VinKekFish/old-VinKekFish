using cryptoprime.VinKekFish;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using vinkekfish.keccak.keccak_20200918;

using static cryptoprime.BytesBuilderForPointers;
using static cryptoprime.VinKekFish.VinKekFishBase_etalonK1;

namespace vinkekfish
{
    // Допустимые состояния см. ./VinKekFish_k1_base_20210419_keyGeneration.md
    // Объект не производит ввода-вывода

    /// <summary>
    /// При использовании класса обязательно вызвать Init1, затем Init2.
    /// После использования вызвать Dispose, или использовать конструкцию using
    /// 
    /// Класс предназначен для
    /// 1. Генерации ключей на основе основного ключа и открытого вектора инициализации. См. GetNewKey
    /// 2. Генерации любых последовательностей, в том числе, не повторяемых заново. См. GetNewKey, InputRandom, EnterToBackgroundCycle
    /// </summary>
    public unsafe class VinKekFish_k1_base_20210419_keyGeneration: VinKekFish_k1_base_20210419
    {
        public VinKekFish_k1_base_20210419_keyGeneration()
        {
        }

        public override void Init1(int RoundsForTables, byte * additionalKeyForTables, long additionalKeyForTables_length, byte[] OpenInitVectorForTables = null, int PreRoundsForTranspose = 8)
        {
            if (backgroundThread != null)
                ExitFromBackgroundCycle();

            base.Init1(RoundsForTables: RoundsForTables, additionalKeyForTables: additionalKeyForTables, additionalKeyForTables_length: additionalKeyForTables_length, OpenInitVectorForTables: OpenInitVectorForTables, PreRoundsForTranspose: PreRoundsForTranspose);
        }

        public override void Init2(byte * key, ulong key_length, byte[] OpenInitVector, int Rounds = NORMAL_ROUNDS, int RoundsForEnd = NORMAL_ROUNDS, int RoundsForExtendedKey = NORMAL_ROUNDS)
        {
            base.Init2(key: key, key_length: key_length, OpenInitVector: OpenInitVector, Rounds: Rounds, RoundsForEnd: RoundsForEnd, RoundsForExtendedKey: RoundsForExtendedKey);
        }

        public const int key_block_size = 64;

        /// <summary>Функция генерирует ключи шифрования. После инициализации Init1 и Init2, функция готова к использованию без дополнительных вызовов</summary>
        /// <param name="len">Количество байтов, которые сгенерировать</param>
        /// <param name="blockLen">Длина блока генерации в байтах, не более BLOCK_SIZE. По умолчанию, длина уменьшена до 64 байтов (для надёжности)</param>
        /// <param name="allocator">Метод выделения памяти для ключа. Если null, то AllocHGlobal_AllocatorForUnsafeMemory</param>
        /// <param name="CountOfRounds">Количество раундов для генерации</param>
        /// <returns>Сгенерированный ключ</returns>
        public Record GetNewKey(long len = RECOMMENDED_KEY, long blockLen = key_block_size, AllocatorForUnsafeMemoryInterface allocator = null, int CountOfRounds = VinKekFishBase_etalonK1.NORMAL_ROUNDS)
        {
            if (isInited2 != true)
                throw new Exception("VinKekFish_k1_base_20210419_keyGeneration.GetNewKey: isInited2 != true (not initialized)");

            if (allocator == null)
                allocator = new AllocHGlobal_AllocatorForUnsafeMemory();

            if (RTables < CountOfRounds)
                throw new ArgumentOutOfRangeException("VinKekFish_k1_base_20210419_keyGeneration.GetNewKey: RTables < CountOfRounds");

            if (blockLen > BLOCK_SIZE)
                throw new ArgumentOutOfRangeException("VinKekFish_k1_base_20210419_keyGeneration.GetNewKey: blockLen > BLOCK_SIZE");

            if (CountOfRounds < MIN_ROUNDS)
                throw new ArgumentOutOfRangeException("VinKekFish_k1_base_20210419_keyGeneration.GetNewKey: CountOfRounds < MIN_ROUNDS");

            if (blockLen <= 0 || len <= 0)
                throw new ArgumentOutOfRangeException("VinKekFish_k1_base_20210419_keyGeneration.GetNewKey: blockLen <= 0 || len <= 0");


            var  result = allocator.AllocMemory(len);
            byte regime = 0;
            long start  = 0;
            do
            {
                // Режим 255 - это режим отбоя после ввода основного ключа. 254 - это EmptyStep и EmptyStepOverwrite. 253 - InputRandom
                if (regime >= 253)
                    regime = 0;

                NoInputData_ChangeTweak(t0, regime++);
                DoStep(CountOfRounds);

                outputData(result, start, outputLen: len, countToOutput: len - start > blockLen ? blockLen : len - start);
                start += blockLen;
            }
            while (start >= len);

            return result;
        }

        /// <summary>Пропустить один шаг (для большей отбивки от других значений)</summary>
        /// <param name="CountOfRounds">Количество раундов для отбивки</param>
        public void EmptyStep(int CountOfRounds = VinKekFishBase_etalonK1.NORMAL_ROUNDS)
        {
            if (CountOfRounds < MIN_ROUNDS)
                throw new ArgumentOutOfRangeException("VinKekFish_k1_base_20210419_keyGeneration.GetNewKey: CountOfRounds < MIN_ROUNDS");

            NoInputData_ChangeTweak(t0, 254);
            DoStep(CountOfRounds);
        }

        /// <summary>Пропустить один шаг с перезаписью в режиме OVERWRITE (для большей отбивки от других значений и реализации необратимости: после ввода ключа это и так делается)</summary>
        /// <param name="CountOfRounds">Количество раундов для отбивки</param>
        public void EmptyStepOverwrite(int CountOfRounds = VinKekFishBase_etalonK1.NORMAL_ROUNDS)
        {
            if (CountOfRounds < MIN_ROUNDS)
                throw new ArgumentOutOfRangeException("VinKekFish_k1_base_20210419_keyGeneration.GetNewKey: CountOfRounds < MIN_ROUNDS");

            InputData_Overwrite(null, _state, 0, t0, 254);
            DoStep(CountOfRounds);
        }

        /// <summary>Вводит рандомизирующие данные в губку (выполняет криптографические операции)</summary>
        /// <param name="data">Данные для ввода</param>
        /// <param name="data_length">Длина вводимых данных</param>
        /// <param name="CountOfRounds">Количество раундов</param>
        public void InputRandom(byte * data, long data_length, int CountOfRounds = VinKekFishBase_etalonK1.NORMAL_ROUNDS)
        {
            long len = 0;
            while (data_length > 0)
            {
                if (data_length > BLOCK_SIZE)
                    len = BLOCK_SIZE;
                else
                    len = data_length;

                InputData_Xor(data, _state, len, t0, 253);
                data        += len;
                data_length -= len;

                DoStep(CountOfRounds);
            }
        }

        /// <summary>Выполняет один шаг криптографического преобразования. Это сокращённый вызов step без подготовки tweak. Не использовать напрямую</summary>
        /// <param name="CountOfRounds">Количество раундов</param>
        protected void DoStep(int CountOfRounds)
        {
            step
            (
                countOfRounds: CountOfRounds, tablesForPermutations: pTablesHandle, transpose200_3200: _transpose200_3200,
                tweak: t0, tweakTmp: t1, tweakTmp2: t2, state: _state, state2: _state, b: _b, c: _c
            );
        }

        protected readonly Object               backgroundSync   = new object();
        protected volatile Thread               backgroundThread = null;
        protected volatile LightRandomGenerator LightGenerator   = null;
        protected volatile Keccak_PRNG_20201128 keccak_prng      = null;


        public ushort BackgroundSleepTimeout = 0;
        public ushort BackgroundSleepCount   = 0;
        public ushort BackgroundKeccakCount  = 8;

        /// <summary>Количество сгенерированных блоков энтропии.
        /// Считается, что на один байт приходится 1 бит энтропии, то есть на BLOCK_SIZE=512 приходится 512 битов
        /// (рекомендуется ещё уменьшать в пару раз хотя бы).
        /// Лучше всего, если считать, что на один блок приходится 1/8 бита энтропии, то есть при BackgroundKeccakCount = 8 переменная BackgourndGenerated - это количество сгенерированных битов энтропии.
        /// В любом случае, это не надёжный источник рандомизации, вместе с ним необходимо использовать и другие источники, если возможно.
        /// При таком подходе, условно, примерно за 100 секунд на одном ядре с максимальной загрузкой генерируется ключ на 4096 битов длиной, так что производительность всё равно хорошая
        /// BackgroundKeccakCount устанавливает сколько раз срабатывает keccak, прежде чем данные попадут в VinKekFish. BackgroundKeccakCount = 8 уменьшает количество данных в VinKekFish в 8 раз.</summary>
        public ulong  BackgourndGenerated = 0;

        /// <summary>Войти в цикл дополнительной инициализации псевдослучайными значениями.
        /// До вызова ExitFromBackgroundCycle пользователь не должен использовать других методов.
        /// Если есть желание использовать другие методы, то их нужно оборачивать lock (this).
        /// </summary>
        /// <param name="BackgroundSleepTimeout">Thread.Sleep(BackgroundSleepTimeout). Устанавливает значение, на которое поток спит после генерации BackgroundSleepCount блоков keccak (по 64 байта).
        /// BackgroundSleepCount = 0 - самое быстрое, загрузка почти всего процессорного ядра, если оно не занято чем-то ещё.
        /// BackgroundSleepCount = 72 - это загрузка на уровне не выше пары процентов от одного ядра.
        /// Происходит генерация где-то одного блока VinKekFish (BLOCK_SIZE байтов) за секунду или менее (то BackgourndGenerated приращается на единицу за секунду или быстрее) - то есть 1 бит рандомизации в секунду</param>
        /// <param name="BackgroundSleepCount">После таймаута идёт генерация блоков для keccak. На один таймаут приходится BackgroundSleepCount блоков</param>
        /// <param name="generator">Генератор нестойких псевдослучайных чисел, должен генерировать по 64 байта в блок. В ExitFromBackgroundCycle автоматически удаляется</param>
        public void EnterToBackgroundCycle(ushort BackgroundSleepTimeout = 72, ushort BackgroundSleepCount = 8, LightRandomGenerator generator = null)
        {
            if (backgroundThread != null)
                throw new Exception("VinKekFish_k1_base_20210419_keyGeneration.EnterToBackgroundCycle: backgroundThread != null. Call ExitFromBackgroundCycle");

            if (generator == null)
                generator = new LightRandomGenerator(64);

            this.BackgroundSleepTimeout = BackgroundSleepTimeout;
            this.BackgroundSleepCount   = BackgroundSleepCount;
            keccak_prng                 = new Keccak_PRNG_20201128();
            BackgourndGenerated         = 0;

            LightGenerator   = generator;
            backgroundThread = new Thread
            (
                delegate()
                {
                    Record data      = null;
                    int    keccakCnt = 0;
                    int    cnt       = 0;
                    do
                    {
                        Thread.Sleep(BackgroundSleepTimeout);
                        if (BackgroundSleepCount < 1)
                            BackgroundSleepCount = 1;

                        for (cnt = 0; cnt < BackgroundSleepCount; cnt++)
                        {
                            Thread.Sleep(0);
                            lock (backgroundSync)
                            {
                                if (LightGenerator == null)
                                    break;

                                LightGenerator.WaitForGenerator(64);
                                if (LightGenerator.ended)
                                    break;

                                // System.IO.File.AppendAllText(@"Z:\tmp.txt", LightGenerator.GeneratedBytes.ToString());

                                lock (this)
                                {
                                    // Каждые BackgroundKeccakCount мы сохраняем результат. Остальные разы просто вводим данные без сохранения
                                    keccakCnt++;
                                    keccak_prng.InputBytes(LightGenerator.GeneratedBytes, LightGenerator.GeneratedBytes.len);
                                    keccak_prng.calcStep(keccakCnt >= BackgroundKeccakCount);

                                    if (keccakCnt >= BackgroundKeccakCount)
                                        keccakCnt = 0;

                                    if (keccak_prng.outputCount >= BLOCK_SIZE)
                                    {
                                        data = keccak_prng.output.getBytesAndRemoveIt(  AllocHGlobal_allocator.AllocMemory(BLOCK_SIZE)  );
                                        InputRandom(data, data.len, MIN_ROUNDS);
                                        data.Dispose();
                                        BackgourndGenerated++;
                                        Monitor.PulseAll(backgroundSync);
                                    }
                                }

                                LightGenerator.ResetGeneratedBytes();
                                GC.Collect();       // Иначе бывает так,что программа занимает лишнюю системную память
                                GC.WaitForPendingFinalizers();
                            }
                        }
                    }
                    while (LightGenerator != null);
                }
            );

            backgroundThread.Priority = ThreadPriority.Lowest;
            backgroundThread.Start();
        }

        public void ExitFromBackgroundCycle()
        {
            lock (backgroundSync)
            {
                LightGenerator?.Dispose();
                keccak_prng   ?.Dispose();

                LightGenerator = null;
                keccak_prng    = null;
            }

            InputData_Overwrite(null, _state, 0, t0, 253);
            DoStep(NORMAL_ROUNDS);
        }

        public override void Dispose(bool disposing)
        {
            if (LightGenerator != null)
                ExitFromBackgroundCycle();

            base.Dispose(disposing);
        }
    }
}
