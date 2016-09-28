using System;

namespace Sharpin2 {
    [AttributeUsage(AttributeTargets.Class)]
    public class Mixin : Attribute {
        public Type targetType;
        public string target;
        public int priority;

        public Mixin() {

        }

        public Mixin(Type targetType) {
            this.targetType = targetType;
        }
    }
}
