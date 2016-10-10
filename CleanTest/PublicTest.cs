namespace CleanTest {
    public class PublicTest {
        protected string testString = "Secret!";
        private int testInt = 0;

        public bool TestInject() {
            System.Console.WriteLine("> PublicTest.TestInject");
            return false;
        }

        public void TestInjectSimple() {
            int mouseX = 111 + 666;
            System.Console.WriteLine("> PublicTest.TestInjectSimple: " + mouseX);
        }

        public void TestInjectCancelTarget() {
            string test = "Death";
            // Some checks and stuff
            if(test == "Death") {
                System.Console.WriteLine(TestOverwrite());
            }

            System.Console.WriteLine("Also doing other stuff though: " + testInt);
            // Some other stuff here
            testInt++;
        }

        public int TestInjectionPointReturn() {
            return 9001;
        }

        public void TestImplements(PublicTest pt) {
            System.Console.WriteLine("Oops, the overwrite for TestImplements failed?");
        }

        public string TestOverwrite() {
            return "Failure!";
        }

        public bool TestingThisToo() {
            return true;
        }

    }
}
