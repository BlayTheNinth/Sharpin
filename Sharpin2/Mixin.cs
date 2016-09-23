using System;

namespace Sharpin2 {
    [AttributeUsage(AttributeTargets.Class)]
    public class Mixin : Attribute {
        public Type targetType;
        public string targets;
        public int priority;

        public Mixin(Type targetType) {
            this.targetType = targetType;
        }
    }
}
