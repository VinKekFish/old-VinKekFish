using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

using static cryptoprime.BytesBuilderForPointers;

namespace cryptoprime
{
    /// <summary>
    /// BytesBuilderStatic
    /// BytesBuilderForPointers, реализованный с циклическим буфером
    /// Класс позволяет собирать большой блок байтов из более мелких
    /// Класс непотокобезопасный (при его использовании необходимо синхронизировать доступ к классу вручную)
    /// </summary>
    public unsafe partial class BytesBuilderStatic: IDisposable
    {
        public long size;
        public readonly AllocatorForUnsafeMemoryInterface allocator = null;

        public const int MIN_SIZE = 2;
        public BytesBuilderStatic(long Size, AllocatorForUnsafeMemoryInterface allocator = null)
        {
            if (Size < MIN_SIZE)
                throw new ArgumentOutOfRangeException("BytesBuilderStatic.BytesBuilderStatic: Size < MIN_SIZE");

            this.allocator = allocator ?? new AllocHGlobal_AllocatorForUnsafeMemory();

            Resize(Size);
        }

        public void Resize(long Size)
        {
            var newRegion = allocator.AllocMemory(Size);
            var oldRegion = region;

            if (oldRegion != null)
            {
                ReadBytesTo(newRegion.array, count);
                oldRegion.Dispose();
            }

            region = newRegion;
            size   = Size;
            bytes  = region.array;
            after  = bytes + region.len;
            Start  = 0;
            End    = Count;
        }

        public void ReadBytesTo(byte* target, long count)
        {
            if (count > Count)
                throw new ArgumentOutOfRangeException("ReadBytesTo: count > Count");

            var s1 = bytes + Start;
            var l1 = len1;
            var l2 = len2;

            if (count <= 0)
                throw new ArgumentOutOfRangeException();
            if (l1 + l2 != this.count)
                throw new Exception("ReadBytesTo: Fatal algorithmic error: l1 + l2 != this.count");

            BytesBuilder.CopyTo(l1, count, s1, target);

            if (l2 > 0 && count - l1 > 0)
            BytesBuilder.CopyTo(l2, count - l1, bytes, target + l1);
        }

        public void WriteBytes(byte* source, long countToWrite)
        {
            if (count + countToWrite > size)
                throw new ArgumentOutOfRangeException("WriteBytes: count + countToWrite > size");

            if (End >= Start)
            {
                var s1 = bytes + End;
                var l1 = after - s1;

                var A  = BytesBuilder.CopyTo(countToWrite, l1, source, s1);
                count += A;
                End   += A;

                if (A != l1 && A != countToWrite)
                    throw new Exception("WriteBytes: Fatal algorithmic error: A != l1 && A != countToWrite");

                if (End >= size)
                {
                    if (End == size)
                        End = 0;
                    else
                        throw new Exception("WriteBytes: Fatal algorithmic error: End > size");
                }

                if (A < countToWrite)
                    WriteBytes(source + A, countToWrite - A);
            }
            else
            {
                var s1 = bytes + End;
                var l1 = (bytes + Start) - s1;

                var A  = BytesBuilder.CopyTo(countToWrite, l1, source, s1);
                count += A;
                End   += A;

                if (A != countToWrite)
                    throw new Exception("WriteBytes: Fatal algorithmic error: A != countToWrite");

                if (End > Start)
                    throw new Exception("WriteBytes: Fatal algorithmic error: End > Start");
            }
        }

        protected byte * bytes  = null;
        protected byte * after  = null;        // Поле, указывающее на первый байт после конца массива
        protected Record region = null;

        protected long count = 0;
        /// <summary>Количество всех сохранённых байтов в этом объекте</summary>
        public long Count  => count;

        protected long Start = 0, End = 0;   // End - это индекс следующего добавляемого байта. Для Start = 0 поле End должно указывать размер

        public byte * this[long index]
        {
            get
            {
                if (index >= count)
                    throw new ArgumentOutOfRangeException();

                var p = bytes + index;

                if (p < after)
                {
                    return p;
                }
                else // End <= Start
                {
                    var len1 = size - Start;    // Длина первой (правой) части массива в циклическом буфере
                    index   -= len1;

                    p = bytes + index;

                    return p;
                }
            }
        }

        /// <summary>Длина данных, приходящихся на правый сегмент данных</summary>
        public long len1
        {
            get
            {
                if (End > Start)
                {
                    return End - Start;
                }
                else
                {
                    var r = (after - (bytes + Start));

                    if (count == 0)
                        return 0;

                    return r;
                }
            }
        }

        /// <summary>Длина данных, приходящихся на левый сегмент данных</summary>
        public long len2
        {
            get
            {
                if (End > Start || count == 0)
                    return 0;

                return End;
            }
        }

        /// <summary>Добавляет блок в объект</summary><param name="bytesToAdded">Добавляемый блок данных. Содержимое копируется</param>
        public void add(byte * bytesToAdded, long len)
        {
            if (count + len > size)
                throw new IndexOutOfRangeException("BytesBuilderStatic.add: count + len > size: many bytes to add");

            WriteBytes(bytesToAdded, len);
        }

        /// <summary>Добавляет массив в сохранённые значения без копирования. Массив будет автоматически очищен и освобождён после окончания использования</summary>
        /// <param name="rec">Добавляемый массив (не копируется, будет уничтожен автоматически при уничтожении BytesBuilder)</param>
        public void add(Record rec)
        {
            add(rec.array, rec.len);
        }

        /// <summary>Обнуляет объект</summary>
        /// <param name="fast">fast = <see langword="false"/> - обнуляет все байты сохранённые в массиве</param>
        public void Clear(bool fast = false)
        {
            count = 0;
            Start = 0;
            End   = 0;

            if (!fast)
                BytesBuilder.ToNull(size, bytes);
        }

        /// <summary>Создаёт массив байтов, включающий в себя все сохранённые массивы</summary>
        /// <param name="resultCount">Размер массива-результата (если нужны все байты resultCount = -1)</param>
        /// <param name="resultA">Массив, в который будет записан результат. Если resultA = null, то массив создаётся</param>
        /// <returns></returns>
        public Record getBytes(long resultCount = -1, Record resultA = null, AllocatorForUnsafeMemoryInterface allocator = null)
        {
            if (resultCount == -1)
                resultCount = count;

            if (resultCount > count)
            {
                throw new System.ArgumentOutOfRangeException("resultCount", "resultCount is too large: resultCount > count");
            }

            if (resultA != null && resultA.len < resultCount)
                throw new System.ArgumentOutOfRangeException("resultA", "resultA is too small");

            var result = resultA ?? allocator?.AllocMemory(resultCount) ?? this.allocator.AllocMemory(resultCount);

            ReadBytesTo(result.array, result.len);

            return result;
        }

        public void RemoveBytes(long len)
        {
            if (len > count)
                throw new ArgumentOutOfRangeException();

            // Обнуление удаляемых байтов
            // Не сказать, что это очень эффективно
            for (long i = 0; i < len; i++)
            {
                *this[i] = 0;
            }

            Start += len;
            count -= len;
            if (Start >= size)
            {
                Start -= size;

                if (Start + count != End)
                    throw new Exception("BytesBuilderStatic.RemoveBytes: Fatal algorithmic error: Start + count != End");
            }
        }

        /// <summary>Создаёт массив байтов, включающий в себя resultCount символов, и удаляет их с очисткой из BytesBuilder</summary>
        /// <param name="result">Массив, в который будет записан результат. Уже должен быть выделен. result != <see langword="null"/>. Длина запрошенных данных устанавливается полем len этой записи</param>
        /// <returns>Запрошенный результат (первые resultCount байтов), этот возвращаемый результат равен параметру result</returns>
        public Record getBytesAndRemoveIt(Record result)
        {
            ReadBytesTo(result, result);
            RemoveBytes(result);

            return result;
        }

        public Record getBytesAndRemoveIt(Record result, int count)
        {
            if (count > result.len)
                throw new ArgumentOutOfRangeException("BytesBuilderStatic.getBytesAndRemoveIt: count > result.len");

            ReadBytesTo(result.array, count);
            RemoveBytes(count);

            return result;
        }

        public virtual void Dispose(bool disposing = true)
        {
            if (region == null)
                return;

            region?.Dispose();
            region = null;

            if (!disposing)
                throw new Exception("~BytesBuilderStatic: region != null");
        }

        public void Dispose()
        {
            Dispose(true);
        }

        ~BytesBuilderStatic()
        {
            Dispose(false);
        }
    }
}
