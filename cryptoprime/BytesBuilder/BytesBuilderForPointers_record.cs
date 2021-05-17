using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace cryptoprime
{
    public unsafe partial class BytesBuilderForPointers
    {
        // Документация по состояниям ./Documentation/BytesBuilderForPointers.Record.md
        public unsafe class Record: IDisposable, ICloneable
        {
            public          byte *   array = null;
            public          long     len   = 0;

            public GCHandle handle = default;
            public IntPtr   ptr    = default;

            public AllocatorForUnsafeMemoryInterface allocator = null;

            #if DEBUG
            public        string DebugName = null;

            public        long   DebugNum  = 0;
            public static long   CurrentDebugNum = 0;
            #endif

            /// <summary>Этот метод вызывать не надо. Используйте AllocatorForUnsafeMemoryInterface.AllocMemory</summary>
            public Record()
            {
                #if DEBUG
                DebugNum = CurrentDebugNum++;
                // if (DebugNum == 7)
                // DebugName = new System.Diagnostics.StackTrace().ToString();
                #endif
            }

            /// <summary>Выводит строковое представление для отладки</summary>
            public override string ToString()
            {
                var sb = new StringBuilder();

                sb.AppendLine($"length = {len}; ");
                if (array != null)
                {
                    for (int i = 0; i < len; i++)
                        sb.Append(array[i].ToString("D3") + "  ");
                }
                else
                {
                    sb.Append("array == null");
                }

                return sb.ToString();
            }

            public string ToString(int maxLen = int.MaxValue)
            {
                var sb = new StringBuilder();

                sb.AppendLine($"length = {len}");
                if (array != null)
                {
                    for (int i = 0; i < len && i < maxLen; i++)
                        sb.Append(array[i].ToString("D3") + "  ");
                }
                else
                {
                    sb.Append("array == null");
                }

                var str = sb.ToString();
                if (str.Length > maxLen)
                    str = str.Substring(0, maxLen);

                return str;
            }

            public object Clone()
            {
                return CloneBytes(this);
            }

            public Record Clone(long start = 0, long PostEnd = -1, AllocatorForUnsafeMemoryInterface allocator = null)
            {
                // allocator будет взят из this, если он null
                return CloneBytes(this, allocator, start, PostEnd);
            }

            /// <summary>Копирует запись, но без копированя массива и без возможности его освободить. Массив должен быть освобождён в копируемой записи только после того, как будет закончено использование копии</summary>
            /// <param name="len">Длина массива либо -1, если длина массива такая же, как копируемой записи</param>
            /// <returns>Новая запись, указывающая на тот же самый массив</returns>
            public Record NoCopyClone(long len = -1)
            {
                if (len < 0)
                    len = this.len;

                return new Record()
                {
                    len       = len,
                    array     = this.array,
                    allocator = null
                };
            }

            /// <summary>Очищает выделенную область памяти (пригодно для последующего использования). Для освобождения памяти используйте Dispose()</summary>
            public void Clear()
            {
                if (array != null)
                    BytesBuilder.ToNull(len, array);
            }

            /// <summary>Если true, то объект уже уничтожен</summary>
            public bool isDisposed = false;     // Оставлено public, чтобы обеспечить возможность повторного использования того же объекта

            /// <summary>Очищает и освобождает выделенную область памяти</summary>
            // TODO: протестировать на двойной вызов (должно всё работать)
            public void Dispose()
            {
                Dispose(true);
                // GC.SuppressFinalize(this);
            }

            /// <summary>Вызывает Dispose()</summary>
            public void Free()
            {
                Dispose();
            }

            /// <summary>Очищает массив и освобождает выделенную под него память</summary>
            /// <param name="disposing"></param>
            protected virtual void Dispose(bool disposing)
            {
                bool allocatorExists = allocator != null || array != null;

                Clear();
                allocator?.FreeMemory(this);

                len        = 0;
                array     = null;
                ptr       = default;
                handle    = default;
                allocator = null;

                isDisposed = true;

                // TODO: Проверить, что это исключение действительно работает, то есть оно будет залогировано при окончании программы
                // Если аллокатора нет, то и вызывать Dispose не обязательно
                if (!disposing && allocatorExists)
                    throw new Exception("BytesBuilderForPointers.Record ~Record() executed");
            }

            ~Record()
            {
                if (!isDisposed)
                    Dispose(false);
            }

            public static implicit operator byte * (Record t)
            {
                return t.array;
            }

            public static implicit operator ushort * (Record t)
            {
                return (ushort *) t.array;
            }

            public static implicit operator ulong * (Record t)
            {
                return (ulong *) t.array;
            }

            public static Record operator +(Record a, long len)
            {
                return new Record
                {
                    allocator = null,
                    array     = a.array + a.len,
                    len       = len
                };
            }
        }

        /// <summary>Интерфейс описывает способ выделения памяти. Реализация: AllocHGlobal_AllocatorForUnsafeMemory</summary>
        public interface AllocatorForUnsafeMemoryInterface
        {
            /// <summary>Выделяет память. Память может быть непроинициализированной</summary>
            /// <param name="len">Размер выделяемого блока памяти</param>
            /// <returns>Описатель выделенного участка памяти, включая способ удаления памяти</returns>
            public Record AllocMemory(long len);

            /// <summary>Освобождает выделенную область памяти. Не очищает память (не перезабивает её нулями). Должен вызываться автоматически в Record</summary>
            /// <param name="recordToFree">Память к освобождению</param>
            public void   FreeMemory (Record recordToFree);

            /// <summary>Производит фиксацию в памяти массива (интерфейс должен реализовывать либо AllocMemory(long), либо этот метод)</summary>
            /// <param name="array">Исходный массив</param>
            /// <returns>Зафиксированный массив</returns>
            public Record FixMemory(byte[] array);

            /// <summary>Производит фиксацию в памяти объекта, длиной length байтов</summary>
            /// <param name="array">Закрепляемый объект</param>
            /// <param name="length">Длина объекта в байтах. Длины массивов необходимо домножать на размер элемента массива</param>
            /// <returns></returns>
            public Record FixMemory(object array, long length);
        }

        public class AllocHGlobal_AllocatorForUnsafeMemory : AllocatorForUnsafeMemoryInterface
        {
            /// <summary>Выделяет память. Память может быть непроинициализированной</summary>
            /// <param name="len">Длина выделяемого участка памяти</param>
            /// <returns>Описатель выделенного участка памяти, включая способ удаления памяти</returns>
            public Record AllocMemory(long len)
            {
                if (len > int.MaxValue)
                    throw new ArgumentOutOfRangeException("BytesBuilderForPointers.AllocatorForUnsafeMemory.AllocatorForUnsafeMemory: len > int.MaxValue");

                // ptr никогда не null, если не хватает памяти, то будет OutOfMemoryException
                var ptr = Marshal.AllocHGlobal(  (int) len  );
                return new Record() { len = len, array = (byte *) ptr.ToPointer(), ptr = ptr, allocator = this };
            }

            /// <summary>Освобождает выделенную область памяти. Не очищает память (не перезабивает её нулями)</summary>
            /// <param name="recordToFree">Память к освобождению</param>
            public void FreeMemory(Record recordToFree)
            {
                Marshal.FreeHGlobal(recordToFree.ptr);
            }

            /// <summary>Не реализовано</summary>
            public Record FixMemory(byte[] array)
            {
                throw new NotImplementedException();
            }

            /// <summary>Не реализовано</summary>
            public Record FixMemory(object array, long length)
            {
                throw new NotImplementedException();
            }
        }

        public class Fixed_AllocatorForUnsafeMemory : AllocatorForUnsafeMemoryInterface
        {
            /// <summary>Выделяет память с помощью сборщика мусора, а потом фиксирует её. Это работает медленнее раза в 3, чем AllocHGlobal_AllocatorForUnsafeMemory</summary>
            public Record AllocMemory(long len)
            {
                var b = new byte[len];
                return FixMemory(b);
            }

            /// <summary>Освобождает выделенную область памяти. Не очищает память (не перезабивает её нулями)</summary>
            /// <param name="recordToFree">Память к освобождению</param>
            public void FreeMemory(Record recordToFree)
            {
                recordToFree.handle.Free();
            }

            /// <summary>Производит фиксацию в памяти массива (интерфейс должен реализовывать либо AllocMemory(long), либо этот метод)</summary>
            /// <param name="array">Исходный массив</param>
            /// <returns>Зафиксированный массив</returns>
            public Record FixMemory(byte[] array)
            {
                return FixMemory(array, array.LongLength);
            }

            public Record FixMemory(ushort[] array)
            {
                return FixMemory(array, array.LongLength * sizeof(ushort));
            }

            public Record FixMemory(object array, long length)
            {
                var h = GCHandle.Alloc(array, GCHandleType.Pinned);
                var p = h.AddrOfPinnedObject();

                return new Record()
                {
                    len       = length,
                    ptr       = p,
                    array     = (byte *) p.ToPointer(),
                    handle    = h,
                    allocator = this
                };
            }
        }
    }
}
