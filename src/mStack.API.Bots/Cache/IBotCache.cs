using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mStack.API.Bots.Cache
{
    public interface IBotCache
    {
        void CacheForUser(string key, object value, string user, int seconds = 3600);
        T RetrieveForUser<T>(string key, string user);
        void DeleteForUser(string key, string user);
        void Cache(string key, object value, int seconds = 3600);
        T Retrieve<T>(string key);
        void Delete(string key);
    }
}
