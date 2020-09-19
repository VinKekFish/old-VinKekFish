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
            tasks.Enqueue(task);
        }

        public void StartTests()
        {
            // StringBuilder здесь для проверки сторонними приложениями что строка будет перезатёрта
            // https://www.cheatengine.org/ - для Windows
            // https://hackware.ru/?p=10645 - mxtract для Linux
            var sb = new StringBuilder();
            // Эту строку надо искать в оперативной памяти
            // Pe9a3aK8z0Avoie4c6d5kzHOkZmOQPtEdcCVVElbmGZAj6z1kKuBBrgq8Mwd3gOY
            sb.Append("Pe9a3aK8z0Avoie4");
            sb.Append("c6d5kzHOkZmOQPt");
            sb.Append("EdcCVVElbmGZA");
            sb.Append("j6z1kKuBBrgq8Mwd3gOY");
            sb.ToString();
            sb.Clear();

            var k = new Keccak_20200918();
            k.Clear();
        }
    }
}
