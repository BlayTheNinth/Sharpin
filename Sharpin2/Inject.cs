using System;

namespace Sharpin2 {
    [AttributeUsage(AttributeTargets.Method)]
    public class Inject : Attribute {
        public string method;
        public string at;
        public bool cancellable;
        public string cancelTarget;
        public int expectedInjections;
    }
}
