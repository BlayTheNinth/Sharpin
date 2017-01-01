using System;

namespace Sharpin2 {
    [AttributeUsage(AttributeTargets.Method)]
    public class Inject : Attribute {
        public string Method;
        public string At;
        public bool Cancellable;
        public string CancelTarget;
        public int ExpectedInjections;
    }
}
