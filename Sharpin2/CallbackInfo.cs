namespace Sharpin2 {
    public class CallbackInfo {
        public bool IsCancelled { get; private set; }

        public void Cancel() {
            this.IsCancelled = true;
        }
    }
}
