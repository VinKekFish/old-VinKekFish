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
    class EmtyString
    {
        TestTask task;
        public EmtyString(ConcurrentQueue<TestTask> tasks)
        {
            task = new TestTask("BytesBuilder.ClearString", StartTests);
            tasks.Enqueue(task);
        }

        public void StartTests()
        {
            List<string> testString = new List<string>(128);
            testString.Add("");
            testString.Add("1");
            testString.Add(" ");
            testString.Add("\t");
            testString.Add("0123456789");
            testString.Add("abcde[]");
            testString.Add("абвгдеёждиклмя");
            testString.Add("ʦʫ");

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
