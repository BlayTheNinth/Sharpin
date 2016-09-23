using System;

namespace Sharpin2 {
    [AttributeUsage(AttributeTargets.Parameter)]
    public class CaptureLocal : Attribute {
        public int index;
        public Type type;

        public CaptureLocal(int index, Type type) {
            this.index = index;
            this.type = type;
        }
    }
}
