using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using cryptoprime;
using static cryptoprime.keccak;

namespace vinkekfish.keccak.keccak_20200918
{
    public unsafe class Keccak_PRNG_20201128 : Keccak_base_20200918
    {
        // TODO: сделать тесты на Clone
        public override Keccak_abstract Clone()
        {
            var result = new Keccak_PRNG_20201128();

            // Очищаем C и B, чтобы не копировать какие-то значения, которые не стоит копировать, да и хранить тоже
            clearOnly_C_and_B();

            // Копировать всё состояние не обязательно. Но здесь, для надёжности, копируется всё (в т.ч. ранее очищенные нули)
            for (int i = 0; i < State.LongLength; i++)
                    result.State[i] = State[i];

            return result;
        }


        /// <summary>Сюда можно добавлять байты для ввода</summary>
        protected readonly BytesBuilder INPUT = new BytesBuilder(); // Не забыт ли вызов InputBytesImmediately при добавлении сюда?
        public const int InputSize = 64;

        /// <summary>Это массив для немедленного введения в Sponge на следующем шаге</summary>
        protected readonly byte[] inputTo = new byte[InputSize];
        protected          bool   inputReady  = false;

        /// <summary>Ввести рандомизирующие байты</summary>
        /// <param name="bytesToInput">Рандомизирующие байты</param>
        public void InputBytes(byte[] bytesToInput)
        {
            INPUT.add(bytesToInput);
            InputBytesImmediately();
        }

        /// <summary>Ввести секретный ключ</summary>
        /// <param name="key">Ключ, кратный 64-ём байтам</param>
        public void InputKey(byte[] key)
        {
            if (INPUT.countOfBlocks > 0)
                throw new ArgumentException("key must be input before the generation or input an initialization vector", "key");

            INPUT.add(key);
            InputBytesImmediately();
            do
            {
                calcStep(Overwrite: false);
            }
            while (inputReady);

            // Завершаем ввод ключа конструкцией Overwrite, которая даёт некую необратимость состояния в отношении ключа
            BytesBuilder.ToNull(inputTo);
            inputReady = true;
            calcStep(Overwrite: true);

            if (INPUT.countOfBlocks > 0)
            {
                INPUT.clear();
                Clear(true);
                throw new ArgumentException("key must be a multiple of 64 bytes", "key");
            }
        }

        public override void Clear(bool GcCollect = false)
        {
            BytesBuilder.ToNull(inputTo);
            output.clear();
            base.Clear(GcCollect);
        }

        protected void InputBytesImmediately()
        {
            if (!inputReady)
            if (INPUT.Count >= InputSize)
            {
                // TODO: сделать тесты на верность getBytesAndRemoveIt и, по возможности, на его использование
                INPUT.getBytesAndRemoveIt(inputTo, InputSize);
                inputReady = true;
            }
        }

        protected readonly BytesBuilder output = new BytesBuilder();

        /// <summary>Выдаёт случайные криптостойкие значения байтов. Выгодно использовать при большом количестве байтов (64 и более)</summary>
        /// <param name="output">Массив, в который записывается результат</param>
        /// <param name="len">Количество байтов, которые необходимо записать</param>
        public void getBytes(byte * output, long len)
        {
            // Проверяем уже готовые байты
            if (this.output.Count > 0)
            {
                var readyLen = this.output.Count;
                if (readyLen > len)
                {
                    readyLen = len;
                }

                var b = this.output.getBytesAndRemoveIt(null, readyLen);
                fixed (byte * bp = b)
                {
                    BytesBuilder.CopyTo(readyLen, readyLen, bp, output);
                    BytesBuilder.ToNull(readyLen, bp);
                }

                len -= readyLen;

                if (len <= 0)
                    return;
            }

            // Если готовых байтов нет, то начинаем вычислять те, что ещё не готовы
            // И сразу же их записываем
            Keccak_abstract.KeccakStatesArray.getStatesArray(out GCHandle handle, this.State, out byte * S, out _, out _, out _, out _, out _, out _);
            try
            {
                while (len > 0)
                {
                    calcStep();
                    Keccak_Output_512(output: output, len: (byte) (len >= 64 ? 64 : len), S: S);
                    len    -= 64;
                    output += 64;
                }
            }
            finally
            {
                Keccak_abstract.KeccakStatesArray.handleFree(handle);
            }
        }
        
        public byte getByte()
        {
            if (this.output.Count <= 0)
            {
                calcStepAndSaveBytes();
            }

            var b = output.getBytesAndRemoveIt(null, 1);

            var result = b[0];
            b[0] = 0;
            return result;
        }

        /// <summary>Выдаёт случайное криптостойкое число от 0 до cutoff включительно</summary>
        /// <param name="cutoff">Максимальное число (включительно) для генерации</param>
        /// <returns>Случайное число в диапазоне [0; cutoff]</returns>
        public ulong getUnsignedInteger(ulong cutoff = ulong.MaxValue)
        {
            while (true)
            {
                if (this.output.Count < 8)
                {
                    calcStepAndSaveBytes();
                }

                var b = output.getBytesAndRemoveIt(null, 8);

                BytesBuilder.BytesToULong(out ulong result, b, 0);
                BytesBuilder.ToNull(b);

                if (cutoff < 0x8000_0000__0000_0000U)
                    result &= 0x7FFF_FFFF__FFFF_FFFFU;  // Сбрасываем старший бит, т.к. он не нужен никогда

                if (result <= cutoff)
                    return result;
            }
        }

        /// <summary>Получает случайное значение в диапазоне, указанном в функции getCutoffForUnsignedInteger</summary>
        /// <param name="min">Минимальное значение</param>
        /// <param name="cutoff">Результат функции getCutoffForUnsignedInteger</param>
        /// <param name="range">Результат функции getCutoffForUnsignedInteger</param>
        /// <returns>Случайное число в указанном диапазоне</returns>
        public ulong getUnsignedInteger(ulong min, ulong cutoff, ulong range)
        {
            var random = getUnsignedInteger(cutoff) % range;

            return random + min;
        }

        /// <summary>Вычисляет параметры для применения в getUnsignedInteger</summary>
        /// <param name="min">Минимальное значение для генерации</param>
        /// <param name="max">Максимальное значнеие для генерации (включительно)</param>
        /// <param name="cutoff">Параметр cutoff для передачи getUnsignedInteger</param>
        // TODO: хорошо протестировать
        public void getCutoffForUnsignedInteger(ulong min, ulong max, out ulong cutoff, out ulong range)
        {
            range = max - min + 1;

            if (range >= 0x8000_0000__0000_0000U)
            {
                cutoff = range;
                return;
            }

            var mod = (0x8000_0000__0000_0000U) % range;

            if (mod == 0)
            {
                cutoff = range;
                return;
            }

            var result = 0x8000_0000__0000_0000U - mod;

            if (result % range != 0)
                throw new Exception();

            cutoff = result;
        }

        public void calcStepAndSaveBytes()
        {
            calcStep(saveBytes: true);
        }

        /// <summary>Расчитывает шаг губки keccak. Если есть InputSize (64) байта для ввода (точнее, inputReady == true), то вводит первые 64-ре байта</summary>
        /// <param name="Overwrite">Если <see langword="true"/>, то вместо xor применяет перезапись внешней части состояния на вводе данных (конструкция Overwrite)</param>
        protected void calcStep(bool saveBytes = false, bool Overwrite = true)
        {
            Keccak_abstract.KeccakStatesArray.getStatesArray(out GCHandle handle, this.State, out byte * S, out byte * B, out byte * C, out byte * Base, out ulong * Slong, out ulong * Blong, out ulong * Clong);
            try
            {
                // InputBytesImmediately();    // Это на всякий случай добавлено
                if (inputReady)
                {
                    fixed (byte * input = inputTo)
                    {
                        if (Overwrite)
                            Keccak_InputOverwrite64_512(message: input, len: InputSize, S: S);
                        else
                            Keccak_Input_512(message: input, len: InputSize, S: S);

                        inputReady = false;
                        InputBytesImmediately();
                    }
                }

                Keccackf(a: Slong, c: Clong, b: Blong);

                if (saveBytes)
                {
                    var result = new byte[64];
                    fixed (byte * output = result)
                        Keccak_Output_512(output: output, len: 64, S: S);

                    output.add(result);
                }
            }
            finally
            {
                Keccak_abstract.KeccakStatesArray.handleFree(handle);
            }
        }
    }
}
