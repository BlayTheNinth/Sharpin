namespace Sharpin2 {
    public class CallbackInfoReturnable<T> : CallbackInfo {
        private T returnValue;
        public T ReturnValue {
            get {
                return returnValue;
            }
            set {
                this.Cancel();
                this.returnValue = value;
            }
        }
    }
}
