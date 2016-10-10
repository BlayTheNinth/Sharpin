using System;

namespace Sharpin2 {
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class Implements : Attribute {
        public Type targetType;

        public Implements(Type targetType) {
            this.targetType = targetType;
        }
    }
}
