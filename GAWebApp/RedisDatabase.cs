using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using StackExchange.Redis;
using GAWebApp.Models;

namespace GAWebApp
{
    public class RedisDatabase
    {
        private ConnectionMultiplexer connection;

        public RedisDatabase()
        {
            try
            {
                string redisURI = Environment.GetEnvironmentVariable("REDIS_URL");
                if(redisURI == null)
                {
                    redisURI = "127.0.0.1:6379";
                }

                connection = ConnectionMultiplexer.Connect(redisURI);
            }
            catch(RedisException exc)
            {
                throw new Exception("Failed to connect to Redis database!", exc);
            }
        }

        public bool GetState(string calculatorID, State state)
        {
            IDatabase database = connection.GetDatabase();
            if(database.KeyExists(calculatorID))
                state.DeserializeFromString(database.StringGet(calculatorID));
            else
            {
                state.context.GenerateDefaultStorage();
                SetState(calculatorID, state);
            }

            return true;
        }

        public async void SetState(string calculatorID, State state)
        {
            // Being asynchronous, the caller can move on without waiting for the store operation to complete.
            // Put another way, the store operation is a fire-and-forget kind of operation.
            IDatabase database = connection.GetDatabase();
            await database.StringSetAsync(calculatorID, state.SerializeToString());
        }
    }
}