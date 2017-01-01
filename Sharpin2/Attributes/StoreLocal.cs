using System;

namespace Sharpin2 {
    [AttributeUsage(AttributeTargets.Parameter)]
    public class StoreLocal : Attribute {
        public int Index;
        public Type Type;

        public StoreLocal(int index, Type type) {
            Index = index;
            Type = type;
        }
    }
}
