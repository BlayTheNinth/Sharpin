using System;

namespace CleanTest {
    class Program {
        static void Main(string[] args) {
            Console.WriteLine("Welcome to the Ultimately Extreme Sharpin2 Test Environment!");
            PublicTest test = new PublicTest();
            if(!test.TestInject()) {
                Console.WriteLine("Testing Inject: Failure!");
            }
            Console.WriteLine("Testing SimpleInject with capture & store:");
            test.TestInjectSimple();
            Console.WriteLine("Testing Overwrite: " + test.TestOverwrite());
            Console.WriteLine("Alright, and now for the grand finale:");
            test.TestInjectCancelTarget();
            Console.WriteLine("doodoodoo");
            test.TestImplements(test);
            Console.ReadLine();
        }
    }
}
