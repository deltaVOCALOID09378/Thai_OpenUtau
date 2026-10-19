using System;
using OpenUtau.Test.Plugins;

namespace QuickTest
{
    class Program
    {
        static void Main(string[] args)
        {
            try {
                Console.WriteLine("Running EndingClusterNasalTest...");
                var test = new EnToJaTest();
                test.EndingClusterNasalTest("ja_cvvc", new string[] { "ば_C4", "a r_C4", "n_C4" });
                Console.WriteLine("EndingClusterNasalTest PASS!");
            } catch (Exception e) {
                Console.WriteLine("EndingClusterNasalTest FAIL!");
                Console.WriteLine(e.ToString());
            }

            try {
                Console.WriteLine("Running PluginRunnerTest.ExecuteTest...");
                var pluginTest = new OpenUtau.Test.Classic.PluginRunnerTest();
                pluginTest.ExecuteTest();
                Console.WriteLine("PluginRunnerTest.ExecuteTest PASS!");
            } catch (Exception e) {
                Console.WriteLine("PluginRunnerTest.ExecuteTest FAIL!");
                Console.WriteLine(e.ToString());
            }
        }
    }
}
