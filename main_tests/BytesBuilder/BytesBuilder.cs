using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using vinkekfish;
using System.Threading;

namespace main_tests
{
    // TODO: Добавить тесты для BytesBuilder
    // TODO: Замерить время, за которое происходит очистка побайтово и по четверным словам
    // TODO: Замерить время, за которое происходит очистка BytesBuilder.FillByBytes и BytesBuilder.ToNull
    class BytesBuilderTests
    {
        TestTask task;
        public BytesBuilderTests(ConcurrentQueue<TestTask> tasks)
        {
            task = new TestTask("BytesBuilder.???", StartTests);
            tasks.Enqueue(task);
        }

        public void StartTests()
        {
        }
    }
}
