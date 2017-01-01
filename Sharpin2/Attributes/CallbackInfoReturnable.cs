namespace Sharpin2 {
    public class CallbackInfoReturnable<T> : CallbackInfo {
        private T _returnValue;
        public T ReturnValue {
            get {
                return _returnValue;
            }
            set {
                Cancel();
                _returnValue = value;
            }
        }
    }
}
