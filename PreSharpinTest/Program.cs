using System;
using Sharpin2;

namespace PreSharpinTest {
    class Program {
        static void Main(string[] args) {
            Console.WriteLine("Presharping CleanTest.exe into CleanTest-presharp.exe");
            PreSharpin.DumpTypes("CleanTest.exe", "CleanTest-Types.txt");
            PreSharpin.ApplyAccessTransformer("CleanTest.exe", "CleanTest-presharp.exe", @"
                # this is a comment
                public-f CleanTest.PrivateTest

                public System.Boolean CleanTest.PrivateTest::IsThisGud()
                public System.Boolean CleanTest.PrivateTest::IsThisGud(System.String)

                public System.String CleanTest.PrivateTest::noseepls # I can see!

                public-f System.Int32 CleanTest.PrivateTest::notouchpls
                ");
            Sharpin sharpin = new Sharpin("CleanTest-presharp.exe", "MixinTest.dll");
            sharpin.GatherMixins();
            sharpin.ApplyMixins();
            sharpin.Write("CleanTest-mixin.exe");
        }
    }
}
