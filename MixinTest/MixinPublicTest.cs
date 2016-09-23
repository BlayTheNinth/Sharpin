using System;

using Sharpin2;
using CleanTest;

namespace MixinTest
{
    [Mixin(typeof(PublicTest))]
    public abstract class MixinPublicTest : PublicTest
    {
        [Inject(method = "System.Boolean CleanTest.PublicTest::TestInject()", at = "HEAD", cancellable = true)]
        public void TestInject(CallbackInfoReturnable<bool> info) {
            Console.WriteLine("Patched: " + this.testString);
            info.ReturnValue = true;
        }

        [Overwrite]
        public new string TestOverwrite() {
            return "Success!";
        }

        [Inject(method = "System.Void CleanTest.PublicTest::TestInjectSimple()", at = "IL_0017: call System.Void System.Console::WriteLine(System.String)")]
        public void TestInjectSimple([CaptureLocal(0, typeof(int))] int mouseX, [StoreLocal(0, typeof(int))] out int outMouseX) {
            Console.WriteLine("Got the local: " + mouseX);
            outMouseX = 1337;
        }

        [Inject(method = "System.Int32 CleanTest.PublicTest::TestInjectionPointReturn()", at = "RETURN")]
        public new void TestInjectionPointReturn() {
            Console.WriteLine("kk cool");
        }

    }
}
