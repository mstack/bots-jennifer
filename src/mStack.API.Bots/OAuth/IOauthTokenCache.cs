using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mStack.API.Bots.OAuth
{
    public interface IOAuthTokenCache
    {
        byte[] Serialize();

        void Deserialize(byte[] serialized);
    }
}
