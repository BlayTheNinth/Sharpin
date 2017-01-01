using System;

namespace Sharpin2 {
    [AttributeUsage(AttributeTargets.Parameter)]
    public class CaptureLocal : Attribute {
        public int Index;
        public Type Type;

        public CaptureLocal(int index, Type type) {
            Index = index;
            Type = type;
        }
    }
}
