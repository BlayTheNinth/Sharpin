using System;

namespace Sharpin2 {
    [AttributeUsage(AttributeTargets.Field)]
    public class CaptureField : Attribute {
        public string field;

        public CaptureField(string field) {
            this.field = field;
        }
    }
}
