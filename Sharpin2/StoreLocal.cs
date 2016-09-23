using System;

namespace Sharpin2 {
    [AttributeUsage(AttributeTargets.Parameter)]
    public class StoreLocal : Attribute {
        public int index;
        public Type type;

        public StoreLocal(int index, Type type) {
            this.index = index;
            this.type = type;
        }
    }
}
