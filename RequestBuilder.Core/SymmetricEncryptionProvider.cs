using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RequestBuilder {
    public enum SymmetricEncryptionProvider {
        [Obsolete("Gradually being deprecated. Use Aes256")] TrippleDes,
        Aes256
    }
}
