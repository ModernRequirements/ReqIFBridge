using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;

namespace ReqIFBridge.Utility
{
    public class StringLocker
    {
        private static readonly ConcurrentDictionary<string, object> _locks =
            new ConcurrentDictionary<string, object>();

        public static object GetLockObject(string s)
        {
            return _locks.GetOrAdd(s, k => new object());
        }
    }
}