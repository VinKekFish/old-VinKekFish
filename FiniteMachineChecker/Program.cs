using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FiniteMachineChecker
{
    class Program
    {
        static void Main(string[] args)
        {
            var f1 = args[0] + ".reachability";
            File.WriteAllText(f1, "");

            try
            {
                var fmc = new FiniteMachineChecker(args[0]);
                File.AppendAllText(  f1,  fmc.ReachabilityToString()  );
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                Console.ReadKey();
            }
        }
    }
}
