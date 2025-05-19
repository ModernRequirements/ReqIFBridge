using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using inteGREAT.Web.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;

namespace ReqIFBridge.Models
{
    public sealed class RedisHelper
    {
        private const string PERSISTENCE_REDIS_CONNECTION_STRING = "Persistence.RedisCache.ConnectionString";
        public const string BASELINE_CACHE = " : Baseline";
        public const string WORKITEMS_PER_USER_CACHE = " : WorkItemsPerUser";
        public const string WORKSPACE_SESSION_CACHE = " : WorkSpaceSession";
        public const string PROJECT_SETTINGS = " : ProjectSettings";
        public const string TEAM_PROJECT = " : TeamProject";
        public const string USER_PROFILE = " : UserProfile";
        public const string RELATION_TYPES = " : RelationTypes";

        private static ConnectionMultiplexer mRedisConnection;
        private static IDatabase mRedisDatabase;

        #region Constructor

        /// <summary>
        /// Initializes the <see cref="RedisHelper"/> class.
        /// </summary>
        static RedisHelper()
        {
            ConnectToDatabase();
        }

        #endregion


        #region Methods

        #region Public Methods

        /// <summary>
        /// Saves the in cache.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="expiry">The expiry.</param>
        /// <returns></returns>
        public static bool SaveInCache(string key, string value, TimeSpan? expiry = null)
        {
            //DebugLogger..LogDebug("RedisHelper.SaveInCache() :: START");
            try
            {
                IDatabase redisDatabase = GetRedisDatabase();
                long lngTicks = DateTime.Now.Ticks;
                DateTime dt;
                bool b = redisDatabase.StringSet(key, value, expiry);
                lngTicks = DateTime.Now.Ticks - lngTicks;
                dt = new DateTime(lngTicks);
                //DebugLogger..LogInfo("Time taken to Save In Redis is: " + dt.Minute + " mins " + dt.Second + " secs " + dt.Millisecond + " millisec.");
                //DebugLogger..LogDebug("RedisHelper.SaveInCache() :: END");
                return b;
            }
            catch (Exception ex)
            {
                //DebugLogger..LogError(ex);
                //DebugLogger..LogDebug("RedisHelper.SaveInCache() :: END");
                return false;
            }
        }

        /// <summary>
        /// Loads from cache.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public static string LoadFromCache(string key)
        {
            //DebugLogger..LogDebug("RedisHelper.LoadFromCache() :: START");
            try
            {
                IDatabase redisDatabase = GetRedisDatabase();
                long lngTicks = DateTime.Now.Ticks;
                DateTime dt;
                string value = redisDatabase.StringGet(key);
                lngTicks = DateTime.Now.Ticks - lngTicks;
                dt = new DateTime(lngTicks);
                //DebugLogger..LogInfo("Time taken to Load from Redis is: " + dt.Minute + " mins " + dt.Second + " secs " + dt.Millisecond + " millisec.");
                //DebugLogger..LogDebug("RedisHelper.LoadFromCache() :: END");
                return value;
            }
            catch (Exception ex)
            {
                //DebugLogger..LogError(ex);
                //DebugLogger..LogDebug("RedisHelper.LoadFromCache() :: END");
                return null;
            }

        }

        /// <summary>
        /// Removes from cache.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public static bool RemoveFromCache(string key)
        {
            //DebugLogger..LogDebug("RedisHelper.RemoveFromCache() :: START");
            try
            {
                IDatabase redisDatabase = GetRedisDatabase();
                long lngTicks = DateTime.Now.Ticks;
                DateTime dt;
                bool b = redisDatabase.KeyDelete(key);
                lngTicks = DateTime.Now.Ticks - lngTicks;
                dt = new DateTime(lngTicks);
                //DebugLogger..LogInfo("Time taken to Remove from Redis is: " + dt.Minute + " mins " + dt.Second + " secs " + dt.Millisecond + " millisec.");
                //DebugLogger..LogDebug("RedisHelper.RemoveFromCache() :: END");
                return b;

            }
            catch (Exception ex)
            {
                //DebugLogger..LogError(ex);
                //DebugLogger..LogDebug("RedisHelper.RemoveFromCache() :: END");
            }
            return false;
        }

        /// <summary>
        /// Sets the key expire.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="expiry">The expiry.</param>
        /// <returns></returns>
        public static bool SetKeyExpire(string key, TimeSpan expiry)
        {
            //DebugLogger..LogDebug("RedisHelper.SetKeyExpire() :: START");
            try
            {
                IDatabase redisDatabase = GetRedisDatabase();
                long lngTicks = DateTime.Now.Ticks;
                DateTime dt;
                bool b = redisDatabase.KeyExpire(key, expiry);
                lngTicks = DateTime.Now.Ticks - lngTicks;
                dt = new DateTime(lngTicks);
                //DebugLogger..LogInfo("Time taken to Set Key Expire is: " + dt.Minute + " mins " + dt.Second + " secs " + dt.Millisecond + " millisec.");
                //DebugLogger..LogDebug("RedisHelper.SetKeyExpire() :: END");
                return b;

            }
            catch (Exception ex)
            {
                //DebugLogger..LogError(ex);
                //DebugLogger..LogDebug("RedisHelper.SetKeyExpire() :: END");
            }
            return false;
        }

        public static bool KeyExist(string key)
        {
            //DebugLogger..LogDebug("RedisHelper.KeyExist() :: START");
            try
            {
                IDatabase redisDatabase = GetRedisDatabase();
                if (redisDatabase == null) return false;
                long lngTicks = DateTime.Now.Ticks;
                DateTime dt;
                bool b = redisDatabase.KeyExists(key);
                lngTicks = DateTime.Now.Ticks - lngTicks;
                dt = new DateTime(lngTicks);
                //DebugLogger..LogInfo("Time taken to check KeyExist is: " + dt.Minute + " mins " + dt.Second + " secs " + dt.Millisecond + " millisec.");
                //DebugLogger..LogDebug("RedisHelper.KeyExist() :: END");
                return b;
            }
            catch (Exception ex)
            {
                //DebugLogger..LogError(ex);
                //DebugLogger..LogDebug("RedisHelper.KeyExist() :: END");
                return false;
            }
        }

        public static bool KeyMove(string key, int databaseId)
        {
            try
            {
                IDatabase redisDatabase = GetRedisDatabase();
                return redisDatabase.KeyMove(key, databaseId);
            }
            catch (Exception ex)
            {
                //DebugLogger..LogError(ex);
                return false;
            }
        }

        public static bool KeyPersist(string key)
        {
            try
            {
                IDatabase redisDatabase = GetRedisDatabase();
                return redisDatabase.KeyPersist(key);
            }
            catch (Exception ex)
            {
                //DebugLogger..LogError(ex);
                return false;
            }
        }

        public static bool KeyRename(string oldKey, string newKey)
        {
            try
            {
                IDatabase redisDatabase = GetRedisDatabase();
                return redisDatabase.KeyRename(oldKey, newKey);
            }
            catch (Exception ex)
            {
                //DebugLogger..LogError(ex);
                return false;
            }
        }

        public static TimeSpan? KeyTimeToLive(string key)
        {
            try
            {
                IDatabase redisDatabase = GetRedisDatabase();
                return redisDatabase.KeyTimeToLive(key);
            }
            catch (Exception ex)
            {
                //DebugLogger..LogError(ex);
                return null;
            }
        }
        public static bool ApplyKeyBasedLock(string key, TimeSpan? expireTime = null)
        {
            //DebugLogger..LogDebug("RedisHelper.ApplyKeyBasedLock() :: START");
            bool isLock = false;
            key = key + "-Apply-lock";
            if (RedisHelper.KeyExist(key))
            {
                Task<bool> task = new Task<bool>(() =>
                {
                    while (RedisHelper.KeyExist(key))
                    {
                        Thread.Sleep(500);
                    }
                    isLock = RedisHelper.SaveInCache(key, "", expireTime);
                    return isLock;
                }
                    );
                task.Start();
                isLock = task.Result;
            }
            else
            {
                isLock = RedisHelper.SaveInCache(key, "", expireTime);
            }
            if (isLock)
            {
                //DebugLogger..LogDebug("Applied Lock for Key : " + key);
            }
            //DebugLogger..LogDebug("RedisHelper.ApplyKeyBasedLock() :: END");
            return isLock;
        }

        public static bool ReleaseKeyBasedLock(string key)
        {
            //DebugLogger..LogDebug("RedisHelper.ReleaseKeyBasedLock() :: START");
            key = key + "-Apply-lock";
            bool isLockRelease = false;
            isLockRelease = RedisHelper.RemoveFromCache(key);
            if (isLockRelease)
            {
                //DebugLogger..LogDebug("Released Lock for Key : " + key);
            }
            //DebugLogger..LogDebug("RedisHelper.ReleaseKeyBasedLock() :: END");
            return isLockRelease;
        }

        #endregion

        #region Private Methods

        private static void ConnectToDatabase()
        {
            try
            {
                mRedisConnection =
                    ConnectionMultiplexer.Connect(ConfigurationManager.AppSettings[PERSISTENCE_REDIS_CONNECTION_STRING]);

                if (mRedisConnection != null)
                {
                    mRedisDatabase = mRedisConnection.GetDatabase();
                }
            }
            catch (Exception ex)
            {
                //DebugLogger..LogError(ex);
            }
        }

        private static IDatabase GetRedisDatabase()
        {
            if (mRedisDatabase == null)
            {
                ConnectToDatabase();
            }

            return mRedisDatabase;
        }

        #endregion

        #endregion


        #region Properties

        #region Public Properties

        public static bool IsRedisRunning
        {
            get
            {
                //DebugLogger..LogDebug("RedisHelper.IsRedisRunning() :: START");

                long lngTicks = DateTime.Now.Ticks;
                             
                RedisKey redisKey = ConfigurationManager.AppSettings[PERSISTENCE_REDIS_CONNECTION_STRING];
                IDatabase redisDatabase = GetRedisDatabase();
                bool isRedisRuning = redisDatabase != null && redisDatabase.IsConnected(redisKey);
                lngTicks = DateTime.Now.Ticks - lngTicks;
                DateTime dt = new DateTime(lngTicks);
                //DebugLogger..LogDebug("RedisHelper.IsRedisRunning() Time taken to check isredisrunning is minute=" + dt.Minute + " sec= " + dt.Second + " milisecs=" + dt.Millisecond);
                //DebugLogger..LogDebug("RedisHelper.IsRedisRunning() :: End");
                return isRedisRuning;
            }
        }

        #endregion

        #endregion


    }
}