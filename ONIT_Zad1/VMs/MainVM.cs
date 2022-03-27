using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONIT_Zad1
{
    internal class MainVM : BaseVM
    {
        public EncryptionVM EncryptionContext { get; } = new EncryptionVM();
        public DecryptionVM DecryptionContext { get; } = new DecryptionVM();
    }
}
