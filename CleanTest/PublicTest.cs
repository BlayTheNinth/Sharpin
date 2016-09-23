namespace CleanTest {
    public class PublicTest {
        protected string testString = "Secret!";

        public bool TestInject() {
            System.Console.WriteLine("> PublicTest.TestInject");
            return false;
        }

        public void TestInjectSimple() {
            int mouseX = 111 + 666;
            System.Console.WriteLine("> PublicTest.TestInjectSimple: " + mouseX);
        }

        public int TestInjectionPointReturn() {
            return 9001;
        }

        public string TestOverwrite() {
            return "Failure!";
        }

    }
}
