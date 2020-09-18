using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using vinkekfish;

namespace main_tests
{
    class EmtyString
    {
        public EmtyString(ConcurrentQueue<TestTask> tasks)
        {
            var task = new TestTask("EmtyString", StartTests);
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

            foreach (var str in testString)
            {
                var str1 = str.Clone();
                BytesBuilder.ClearString(str);
                Console.WriteLine(str1 + " / " + str);
            }
        }
    }
}
