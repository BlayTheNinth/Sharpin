using System;

using Sharpin2;
using CleanTest;

namespace MixinTest
{
    [Mixin(typeof(PublicTest))]
    public abstract class MixinPublicTest : PublicTest
    {
        public string thisWillBreak = "definitely";

        [Inject(method = "System.Boolean CleanTest.PublicTest::TestInject()", at = "HEAD", cancellable = true)]
        public void TestInject(CallbackInfoReturnable<bool> info) {
            Console.WriteLine("Patched: " + this.testString);
            info.ReturnValue = true;
        }

        [Inject(method = "System.Void CleanTest.PublicTest::TestInjectCancelTarget()", at = "HEAD", cancellable = true, cancelTarget = "IL_0039: call System.Void System.Console::WriteLine(System.String)")]
        public void TestInjectCancelTarget(CallbackInfo info) {
            Console.WriteLine("Skip the death!");
            info.Cancel();
        }

        [Overwrite]
        public new string TestOverwrite() {
            //if (this.YouProbablyShouldntDoThis()) {
            if (OtherClass.TestingThisToo(this)) {
                    return "Success!";
            } else {
                return "Oops!";
            }
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

        public bool YouProbablyShouldntDoThis() {
            return OtherClass.TestingThisToo(this) && this.thisWillBreak == "definitely";
        }
    }
}
