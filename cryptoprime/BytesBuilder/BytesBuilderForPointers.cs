﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace cryptoprime
{
    /// <summary>
    /// BytesBuilderForPointers
    /// Класс позволяет собирать большой блок байтов из более мелких
    /// Класс непотокобезопасный (при его использовании необходимо синхронизировать доступ к классу вручную)
    /// </summary>
    public unsafe partial class BytesBuilderForPointers
    {
        /// <summary>Добавленные блоки байтов</summary>
        public readonly List<Record> bytes = new List<Record>();


        /// <summary>Количество всех сохранённых байтов в этом объекте</summary>
        public long Count  => count;

        /// <summary>Количество всех сохранённых блоков, как они были добавлены в этот объект</summary>
        public long countOfBlocks => bytes.Count;

        /// <summary>Получает сохранённых блок с определённым индексом в списке сохранения</summary><param name="number">Индекс в списке</param><returns>Сохранённый блок (не копия, подлинник)</returns>
        public Record getBlock(int number)
        {
            return bytes[number];
        }

        /// <summary>Количество сохранённых байтов</summary>
        long count = 0;

        /// <summary>Добавляет блок в объект</summary><param name="bytesToAdded">Добавляемый блок данных</param>
        /// <param name="index">Куда добавляется блок. По-умолчанию, в конец (index = -1)</param>
        /// <param name="MakeCopy">MakeCopy = true говорит о том, что данные блока будут скопированы (создан новый блок и он будет добавлен). По-умолчанию false - блок будет добавлен без копирования. Это значит, что при изменении исходного блока, изменится и выход, даваемый объектом. Если исходный блок будет обнулён, то будет обнулены и выходные байты из этого объекта, соответствующие этому блоку</param>
        // При добавлении блока важно проверить, верно выставлен параметр MakeCopy и если MakeCopy = false, то блок не должен изменяться
        public void add(byte * bytesToAdded, long len, int index = -1, bool MakeCopy = false, AllocatorForUnsafeMemoryInterface allocator = null)
        {
            Record rec = null;
            if (MakeCopy)
            {
                rec = CloneBytes(bytesToAdded, 0, len, allocator);
            }
            else
                rec = new Record() { len = len, array = bytesToAdded };

            add(rec, index);
        }

        /// <summary>Добавляет массив в сохранённые значения без копирования. Массив будет автоматически очищен и освобождён после окончания использования</summary>
        /// <param name="rec">Добавляемый массив</param>
        /// <param name="index">Индекс позиции, на которую добавляется массив</param>
        public void add(Record rec, int index = -1)
        {
            if (index == -1)
                bytes.Add(rec);
            else
                bytes.Insert((int) index, rec);

            count += rec.len;
        }

        /// <summary>Обнуляет объект</summary>
        /// <param name="fast">fast = <see langword="false"/> - обнуляет все байты сохранённые в массиве</param>
        public void clear(bool fast = false)
        {
            if (!fast)
            {
                foreach (Record e in bytes)
                    e.Dispose();
            }

            count = 0;
            bytes.Clear();
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

            var result = resultA ?? allocator.AllocMemory(resultCount);

            long cursor = 0;
            for (int i = 0; i < bytes.Count; i++)
            {
                if (cursor >= result.len)
                    break;

                BytesBuilder.CopyTo(bytes[i].len, result.len, bytes[i].array, result.array, cursor);
                cursor += bytes[i].len;
            }

            return result;
        }

        /// <summary>Клонирует массив, начиная с элемента start, до элемента с индексом PostEnd (не включая)</summary><param name="B">Массив для копирования</param>
        /// <param name="start">Начальный элемент для копирования</param>
        /// <param name="PostEnd">Элемент, расположенный после последнего элемента для копирования</param>
        /// <returns>Новый массив</returns>
        public static unsafe Record CloneBytes(byte * b, long start, long PostEnd, AllocatorForUnsafeMemoryInterface allocator)
        {
            var result = allocator.AllocMemory(PostEnd - start);
            BytesBuilder.CopyTo(PostEnd, PostEnd - start, b, result.array, 0, -1, start);

            return result;
        }

        public static unsafe Record CloneBytes(Record rec, AllocatorForUnsafeMemoryInterface allocator = null, long start = 0, long PostEnd = -1)
        {
            if (PostEnd < 0)
                PostEnd = rec.len;
            if (allocator == null)
                allocator = rec.allocator;

            if (allocator == null)
                throw new Exception("BytesBuilderForPointers.CloneBytes: allocator == null");

            return CloneBytes(rec.array, start, PostEnd, allocator);
        }

        public static unsafe Record CloneBytes(byte[] b, AllocatorForUnsafeMemoryInterface allocator, long start = 0, long PostEnd = -1)
        {
            if (PostEnd < 0)
                PostEnd = b.LongLength;

            fixed (byte * bb = b)
            {
                return CloneBytes(bb, start, PostEnd, allocator);
            }
        }

        
        /// <summary>Удаляет блок из объекта с позиции position, блок очищается нулями. Эта функция служебная, скорее всего, вам не надо её вызывать</summary>
        /// <returns>Возвращает длину удалённого блока</returns>
        /// <param name="position">Индекс удаляемого блока</param>
        /// <param name="doClear">Если true, то удалённый блок очищается нулями и память, выделенная под него, освобождается ( всё это делается вызовом Record.Dispose() )</param>
        public long RemoveBlockAt(int position, bool doClear = true)
        {
            if (position < 0)
                throw new ArgumentException("position must be >= 0");

            if (position >= bytes.Count)
                throw new ArgumentException("position must be in range");

            var tmp = bytes[position];

            long removedLength = tmp.len;
            bytes.RemoveAt(position);

            if (doClear)
                tmp.Dispose();

            count -= removedLength;
            return removedLength;
        }

        /// <summary>Создаёт массив байтов, включающий в себя resultCount символов, и удаляет их с очисткой из BytesBuilder</summary>
        /// <param name="result">Массив, в который будет записан результат. Уже должен быть выделен. result != <see langword="null"/>. Количество байтов устанавливается длиной массива</param>
        /// <returns>Запрошенный результат (первые resultCount байтов), этот возвращаемый результат равен параметру result</returns>
        // Эта функция может неожиданно обнулить часть массива или массив, сохранённый без копирования (если он где-то используется в другом месте)
        public Record getBytesAndRemoveIt(Record result)
        {
            long   cursor  = 0;
            Record current = null;
            for (int i = 0; i < bytes.Count; )
            {
                if (cursor == result.len)
                    break;

                if (cursor > result.len)
                    throw new System.Exception("Fatal algorithmic error (getBytesAndRemoveIt): cursor > resultCount");

                current = bytes[i];
                if (cursor + current.len > result.len)
                {
                    // Делим массив на две части. Левая уходит наружу, правая остаётся в массиве
                    var left  = result.len - cursor;
                    var right = current.len - left;

                    var bLeft  = current.Clone(0, left, allocator: current.allocator ?? result.allocator);
                    var bRight = current.Clone(left,    allocator: current.allocator ?? result.allocator);
                    
                    RemoveBlockAt(i);

                    bytes.Insert(0, bLeft );
                    bytes.Insert(1, bRight);

                    count += left + right;
                }

                // Осторожно, может быть, что bytes[i] != current
                BytesBuilder.CopyTo(bytes[i].len, result.len, bytes[i].array, result.array, cursor);
                cursor += bytes[i].len;

                RemoveBlockAt(i);
            }
                                
            
            return result;
        }


        /// <summary>Получает 8-мибайтовое целое число из массива. Младший байт по младшему индексу</summary>
        /// <param name="data">Полученное число</param>
        /// <param name="target">Массив с числом</param>
        /// <param name="start">Начальный элемент, по которому расположено число</param>
        /// <param name="length">Полная длина массива, до конца должно оставаться не менее 8-ми байтов</param>
        public unsafe static void BytesToULong(out ulong data, byte * target, long start, long length)
        {
            data = 0;
            if (start < 0 || start + 8 > length)
                throw new IndexOutOfRangeException();

            for (long i = start + 8 - 1; i >= start; i--)
            {
                data <<= 8;
                data += *(target + i);
            }
        }

        ~BytesBuilderForPointers()
        {
            if (bytes.Count > 0)
            {
                clear();
                throw new Exception("~BytesBuilderForPointers: bytes.Count > 0");
            }
        }
    }
}
