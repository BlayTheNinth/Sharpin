using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sharpin2 {
    public class CallbackInfo {
        public bool IsCancelled { get; private set; }

        public void Cancel() {
            this.IsCancelled = true;
        }
    }
}
