using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using vinkekfish;

namespace main_tests
{
    class KeccakClearTest
    {
        TestTask task;
        public KeccakClearTest(ConcurrentQueue<TestTask> tasks)
        {
            task = new TestTask("Keccak_base_20200918.Clear", StartTests);
            task.waitAfter  = true;
            task.waitBefore = true;
            tasks.Enqueue(task);
        }

        public void StartTests()
        {
            CreateString();

            var k = new Keccak_20200918();
            k.Clear(true);
            k.Clear();
        }

        // Раскомментировать для ручного тестирования, если есть необходимость
        private static void CreateString()
        {/*
            // StringBuilder здесь для проверки сторонними приложениями что строка будет перезатёрта
            // https://www.cheatengine.org/ - для Windows (кнопка MemoryView)
            // https://hackware.ru/?p=10645 - mxtract для Linux
            var sb = new StringBuilder();
            // Эту строку надо искать в оперативной памяти - это небольшой блок
            // Pe9a3aK8z0Avoie4c6d5kzHOkZmOQPtEdcCVVElbmGZAj6z1kKuBBrgq8Mwd3gOY
            sb.Append("Pe9a3aK8z0Avoie4");
            sb.Append("c6d5kzHOkZmOQPt");
            sb.Append("EdcCVVElbmGZA");
            sb.Append("j6z1kKuBBrgq8Mwd3gOY");
            var str = new UTF8Encoding().GetBytes(sb.ToString() + "+bigblock");
            sb.Clear();

            List<byte[]> bt = new List<byte[]>();
            for (int i = 0; i < 16; i++)
            {
                try
                {
                    // Выделяем большие блоки - здесь можно проверить большие блоки
                    var b = new byte[256*1024*1024];
                    bt.Add(b);
                    // Копируем туда строку
                    BytesBuilder.CopyTo(str,  b);
                }
                catch
                {}
            }
            bt.Clear();*/
        }
    }
}
