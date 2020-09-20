using System;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using static cryptoprime.keccak;
using cryptoprime;

// В этой сборке специально не включаются оптимизации: хрен знает, что они тут сделают
// С оптимизациями могут быть проблемы с очисткой памяти. Сами функции очистки вынесены в другую библиотеку, с оптимизациями

namespace vinkekfish
{
    /*
     * Этот класс является предком остальных
     * Классы не предназначены для изменений
     * Чтобы их изменять, по хорошему, надо создать новый класс с другой датой создания и добавить его в тесты
     * Наследники этого класса: Keccak_base_*
     * */
    public unsafe abstract class Keccak_abstract
    {
        // Это внутреннее состояние keccak, а также вспомогательные переменные, не являющиеся состоянием
        // Здесь сначала идёт B, потом C, потом S.
        // При перезаписи после конца с высокой вероятностью пострадает S, что даст возможность тестам сделать своё дело
        /// <summary>Внутреннее состояние keccak. Используйте KeccakStatesArray для того, чтобы разбить его на указатели</summary>
        protected readonly byte[] State = new byte[(S_len2 + S_len + S_len2) << 3];
        protected          ulong    d;

        /// <summary>Фиксирует объект State и создаёт на него ссылки
        /// using (var state = new KeccakStatesArray(State))
        /// state.S и другие</summary>
        public class KeccakStatesArray : IDisposable
        {
            // Желательно вызывать ClearAfterUser явно поименованно, чтобы показать, что очистка идёт
            public KeccakStatesArray(byte[] State, bool ClearAfterUse = true)
            {
                this.ClearAfterUse = ClearAfterUse;

                handle = GCHandle.Alloc(State, GCHandleType.Pinned);
                Interlocked.Increment(ref CountToCheck);

                Base  = (byte *) handle.AddrOfPinnedObject().ToPointer();
                B     = Base;
                C     = B + (S_len2 << 3);
                S     = C + (S_len  << 3);

                Slong = (ulong *) S;
                Blong = (ulong *) B;
                Clong = (ulong *) C;
                Size  = State.LongLength;
            }

            public readonly GCHandle handle;
            public readonly byte * S, B, C, Base;
            public readonly ulong * Slong, Blong, Clong;
            public readonly long Size;

            public  readonly bool ClearAfterUse;
            public           bool Disposed
            {
                get;
                protected set;
            }

            protected static   int  CountToCheck = 0;
            /// <summary>В конце программы, после GC.Collect() этот счётчик должен быть 0 (это счётчик того, что все объекты были удалены через Dispose)</summary>
            public    static   int getCountToCheck => CountToCheck;

            public void Dispose()
            {
                if (!Disposed)
                try
                {
                    if (ClearAfterUse)
                        BytesBuilder.ToNull(targetLength: Size, t: Base);

                    Interlocked.Decrement(ref CountToCheck);
                }
                finally
                {
                    Disposed = true;
                    handle.Free();
                }
            }

            ~KeccakStatesArray()
            {
                if (!Disposed)
                    throw new Exception("Keccak_abstract.KeccakStatesArray: not all KeccakStatesArray is disoposed");
            }
        }

        public abstract Keccak_abstract Clone();
        /// <summary>Дополнительно очищает состояние объекта после вычислений.
        /// Рекомендуется вручную вызывать Clear5 и Clear5x5 до выхода из fixed, чтобы GC не успел их переместить (скопировать) до очистки</summary>
        /// <param name="GcCollect">Если true, то override реализации должны дополнительно попытаться перезаписать всю память программы. <see langword="abstract"/> реализация ничего не делает</param>
        public virtual void Clear(bool GcCollect = false)
        {
            ClearState();
        }

        /// <summary>Очищает состояние объекта</summary>
        public virtual void ClearState()
        {
            BytesBuilder.ToNull(State);
            ClearStateWithoutStateField();
        }

        /// <summary>Очищает состояние объекта, но не State</summary>
        public virtual void ClearStateWithoutStateField()
        {
            this.d = 0;
        }

        /// <summary>Инициализирует состояние нулями</summary>
        public virtual void init()
        {
            using (var state = new KeccakStatesArray(State))
                Clear5x5(state.Slong);
            using (var state = new KeccakStatesArray(State))
                Clear5x5(state.Slong);
        }

        /// <summary>Эту функцию можно вызывать после keccak, если нужно состояние S, но хочется очистить B и C</summary>
        public void clearOnly_C_and_B()
        {
            using (var state = new KeccakStatesArray(State))
            {
                Clear5x5(state.Blong);
                Clear5  (state.Clong);
            }
        }

        /// <summary>Этот метод может использоваться для очистки матриц S и B после вычисления последнего шага хеша</summary>
        /// <param name="S">Очищаемая матрица размера 5x5 *ulong</param>
        public unsafe static void Clear5x5(ulong * S)
        {
            var len = S_len2;
            var se  = S + len;
            for (; S < se; S++)
                *S = 0;
        }

        /// <summary>Этот метод может использоваться для очистки вспомогательного массива C</summary>
        /// <param name="C">Очищаемый массив размера 5*ulong</param>
        public unsafe static void Clear5(ulong * C)
        {
            var se  = C + S_len;
            for (; C < se; C++)
                *C = 0;
        }
    }
}
