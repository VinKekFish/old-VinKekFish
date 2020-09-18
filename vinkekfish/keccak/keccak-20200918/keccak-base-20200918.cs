using System;
using System.Collections.Generic;

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
                        BytesBuilder.ToNull(obj);
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
    }
}
