using System;

namespace Sharpin2 {
    [AttributeUsage(AttributeTargets.Field)]
    public class CaptureField : Attribute {
        public string Field;

        public CaptureField(string field) {
            Field = field;
        }
    }
}
