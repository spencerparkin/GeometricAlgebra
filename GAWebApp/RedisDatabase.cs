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

        public class StateCacheEntry
        {
            public State state;
            public DateTime timeStamp;
        }

        private Dictionary<string, StateCacheEntry> stateCacheMap = new Dictionary<string, StateCacheEntry>();

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
            catch(RedisException)
            {
                // Just run without a database.
                connection = null;
            }
        }

        public bool GetState(string calculatorID, out State state)
        {
            state = null;

            if(connection == null)
                return false;

            IDatabase database = connection.GetDatabase();

            if(stateCacheMap.ContainsKey(calculatorID) && database.KeyExists(calculatorID + "_timeStamp"))
            {
                DateTime timeStamp;

                if(DateTime.TryParse(database.StringGet(calculatorID + "_timeStamp"), out timeStamp))
                {
                    if(stateCacheMap[calculatorID].timeStamp >= timeStamp)
                    {
                        state = stateCacheMap[calculatorID].state;
                        return true;
                    }
                }
            }

            state = new State();

            if(database.KeyExists(calculatorID))
                state.DeserializeFromString(database.StringGet(calculatorID));
            else
            {
                state.GenerateDefaultStorage();
                SetState(calculatorID, state);
            }

            return true;
        }

        public async void SetState(string calculatorID, State state)
        {
            if(connection == null)
                return;

            // Being asynchronous, the caller can move on without waiting for the store operation to complete.
            // Put another way, the store operation is a fire-and-forget kind of operation.
            IDatabase database = connection.GetDatabase();
            await database.StringSetAsync(calculatorID, state.SerializeToString());

            DateTime timeStamp = DateTime.Now;
            await database.StringSetAsync(calculatorID + "_timeStamp", timeStamp.ToString());

            if(stateCacheMap.ContainsKey(calculatorID))
                stateCacheMap.Remove(calculatorID);
            
            StateCacheEntry cacheEntry = new StateCacheEntry();
            cacheEntry.timeStamp = timeStamp;
            cacheEntry.state = state;
            stateCacheMap.Add(calculatorID, cacheEntry);
        }
    }
}