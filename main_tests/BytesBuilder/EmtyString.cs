using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using vinkekfish;
using System.Threading;
using cryptoprime;

namespace main_tests
{
    class EmtyString
    {
        readonly TestTask task;
        public EmtyString(ConcurrentQueue<TestTask> tasks)
        {
            task = new TestTask("BytesBuilder.ClearString", StartTests);
            tasks.Enqueue(task);
        }

        public void StartTests()
        {
            var testString = new List<string>(128)
            {
                "", "1", " ", "\t", "0123456789", "abcde[]", "абвгдеёждиклмя", "ʦʫ"
            };

            foreach (var str in testString)
            {
                var str1 = str.Length > 0 ? str.Substring(0, 1) + str.Substring(1) : "";
                BytesBuilder.ClearString(str);
                // Console.WriteLine(str1 + " / " + str);

                testResult(str);
            }
        }

        private void testResult(string str)
        {
            foreach (var c in str)
            {
                if (c != ' ')
                    task.error.Add(new Error() {Message = "EmtyString.testResult for " + str + " get result c != ' ' (№ CEOz6zTVDUDBAcWXlY)"});
            }
        }
    }
}
