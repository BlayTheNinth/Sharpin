using System;

namespace Sharpin2 {
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class Implements : Attribute {
        public Type TargetType;

        public Implements(Type targetType) {
            TargetType = targetType;
        }
    }
}
