using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace vinkekfish
{
    public abstract class Keccak_base_20200918: Keccak_abstract
    {
        public override void Clear(bool GcCollect = true)
        {
            base.Clear(GcCollect);

            if (GcCollect)
            {
                // BytesBuilder.ToNull(zBytes);

                GC.Collect();
                var k = GC.CollectionCount(0);
                try
                {
                    var s   = k;
                    do
                    {
                        // Выносим в отдельную функцию, чтобы всё, что там выделено, выходило из контекста и успешно удалялось
                        AllocFullMemory();

                        // Ждём, пока не произойдёт хотя бы две сборки мусора для нулевого поколения
                        // А если не произойдёт, то зависнем: кривовато, но не зависает
                        s = GC.CollectionCount(0);
                    }
                    while (s <= k + 2);
                }
                catch (OutOfMemoryException)
                {
                    
                }

                GC.Collect();
            }
        }

        private static void AllocFullMemory()
        {
            List<byte[]> bytes = new List<byte[]>(1024);
            try
            {
                int memSize = 1024 * 1024;
                // Выделяем память мегабайтами
                while (memSize > 1)
                {
                    try
                    {
                        var obj = new byte[memSize];
                        bytes.Add(obj);
                        BytesBuilder.ToNull(obj, 0x3737_3737__3737_3737);
                    }
                    catch (OutOfMemoryException)
                    {
                        memSize >>= 1;
                    }
                }
            }
            catch (OutOfMemoryException)
            { }
        }

        /// <summary>DoubleHash.one - обычный хеш 64-ре байта, DoubleHash.two - два раза по 64-ре байта, DoubleHash.full72 - один раз 72 байта</summary>
        public enum DoubleHash {error = 0, one = 1, two = 2, full72 = 72};

        /// <summary>Получает 512-тибитный хеш keccak (не SHA-3, есть отличия)</summary>
        /// <param name="message">Массив для хеширования</param>
        /// <param name="doClear">Если true, то после вычисления хеша выполняется очистка.
        /// Очистка производится с помощью вызова ClearState: это очистка состояния, включая вспомогательные массивы, но без вызова Clear()</param>
        /// <param name="startIndex">Начальный индекс того, что хешируем в message (по-умолчанию - 0)</param>
        /// <param name="countToHash">Количество элементов для хеширования (по-умолчанию - -1 - до конца)</param>
        /// <param name="isInitialized">Если false (по-умолчанию), то функция выполняет инициализацию матрицы S. <see langword="true"/> может быть использовано, если перед хешем была отдельная инициализация, например, модификатором</param>
        /// <param name="forResult">Если null, то массив для хеша будет создан. В противном случае, запись будет произведена в массив forResult</param>
        /// <param name="index">Если forResult != <see langword="null"/>, то запись будет произведена по индексу index</param>
        /// <param name="doubleHash">DoubleHash.one - обычный хеш 64-ре байта, DoubleHash.two - два раза по 64-ре байта, DoubleHash.full72 - один раз 72 байта</param>
        /// <returns>Массив с запрошенным хешем</returns>
        public unsafe byte[] getHash512(byte[] message, bool doClear = true, long startIndex = 0, long countToHash = -1, bool isInitialized = false, byte[] forResult = null, ulong index = 0, DoubleHash doubleHash = DoubleHash.one)
        {
            byte[] result = forResult;
            if (result == null)
            {
                switch (doubleHash)
                {
                    case DoubleHash.one:
                        result = new byte[64];
                        break;
                    case DoubleHash.two:
                        result = new byte[128];     // 128
                        break;
                    case DoubleHash.full72:
                        result = new byte[r_512b];  // 72
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("unknown doubleHash value");
                }
            }

            long mLen = 0;
            if (countToHash < 0)
                mLen = message.LongLength - startIndex;
            else
                mLen = countToHash;

            if (!isInitialized)
                init();

            using (var state = new KeccakStatesArray(State, ClearAfterUse: doClear))
            fixed (byte * R = result, Msg = message)
            {
                ulong * sl = state.Slong;
                ulong * bl = state.Blong;
                ulong * cl = state.Clong;

                byte * s   = state.S;
                byte * b   = state.B;
                byte * c   = state.C;
                byte * r   = R;
                byte * msg = Msg + startIndex;

                if (forResult != null)
                {
                    r += index;
                }


                long len = mLen;
                while (len >= 0)    // len == 0, это если последний блок был 72 байта и теперь идут padding или хешируется пустой массив
                {
                    if (len >= r_512b)
                    {
                        Keccak_Input_512(msg, r_512b, s, false);
                        msg += r_512b;
                        len -= r_512b;

                        Keccackf(sl, cl, bl);
                    }
                    else
                    {
                        Keccak_Input_512(msg, (byte) len, s, true);
                        msg += len;
                        len = 0;

                        Keccackf(sl, cl, bl);
                        break;
                    }
                }

                // TODO: doubleHash
                if (doubleHash == DoubleHash.one)
                    
                switch (doubleHash)
                {
                    case DoubleHash.one:
                        Keccak_Output_512(r, 64, s);
                        break;

                    case DoubleHash.two:
                        Keccak_Output_512(r, 64, s);
                        r += 64;
                        Keccak_Output_512(r, 64, s);
                        break;

                    case DoubleHash.full72:
                        Keccak_Output_512(r, 72, s);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException("unknown doubleHash value");
                }

                if (doClear)
                {
                    // State очищается выше само
                    ClearStateWithoutStateField();
                }
            }

            return result;
        }
    }
}
