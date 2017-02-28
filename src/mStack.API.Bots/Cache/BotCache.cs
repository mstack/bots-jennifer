using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;
using System.Web.Configuration;
using Newtonsoft.Json;

namespace mStack.API.Bots.Cache
{
    [Serializable]
    public class BotCache : IBotCache
    {
        [NonSerialized]
        private static Lazy<ConnectionMultiplexer> _connection = new Lazy<ConnectionMultiplexer>(() =>
        {
            string connectionString = WebConfigurationManager.AppSettings["REDIS_CACHE"];
            return ConnectionMultiplexer.Connect(connectionString);
        });

        private ConnectionMultiplexer Connection
        {
            get { return _connection.Value; }
        }

        public void CacheForUser(string key, object value, string user, int seconds = 3600)
        {
            var redisKey = (RedisKey)$"user:{user}";
            redisKey = redisKey.Append($"key:{key}");

            string jsonValue = JsonConvert.SerializeObject(value);
            IDatabase cache = Connection.GetDatabase();
            cache.StringSet(redisKey, jsonValue);
            cache.KeyExpireAsync(key, TimeSpan.FromSeconds(seconds));
        }

        public T RetrieveForUser<T>(string key, string user)
        {
            var redisKey = (RedisKey)$"user:{user}";
            redisKey = redisKey.Append($"key:{key}");

            IDatabase cache = Connection.GetDatabase();
            string jsonValue = cache.StringGet(redisKey);

            if (!String.IsNullOrEmpty(jsonValue))
                return JsonConvert.DeserializeObject<T>(jsonValue);
            else
                return default(T);
        }

        public void DeleteForUser(string key, string user)
        {
            var redisKey = (RedisKey)$"user:{user}";
            redisKey = redisKey.Append($"key:{key}");

            IDatabase cache = Connection.GetDatabase();
            cache.KeyDelete(redisKey);
        }

        public void Cache(string key, object value, int seconds = 3600)
        {
            string jsonValue = JsonConvert.SerializeObject(value);
            IDatabase cache = Connection.GetDatabase();
            cache.StringSet(key, jsonValue);
            cache.KeyExpireAsync(key, TimeSpan.FromSeconds(seconds));
        }

        public T Retrieve<T>(string key)
        {
            IDatabase cache = Connection.GetDatabase();
            string jsonValue = cache.StringGet(key);

            if (!String.IsNullOrEmpty(jsonValue))
                return JsonConvert.DeserializeObject<T>(jsonValue);
            else
                return default(T);
        }

        public void Delete(string key)
        {
            IDatabase cache = Connection.GetDatabase();
            cache.KeyDelete(key);
        }

    }
}
