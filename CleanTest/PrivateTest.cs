namespace CleanTest {
    sealed class PrivateTest {
        private string noseepls = "noseepls";
        private readonly int notouchpls = 123;

        private bool IsThisGud() {
            return true;
        }

        private bool IsThisGud(string test) {
            return test == "yes";
        }
    }
}
