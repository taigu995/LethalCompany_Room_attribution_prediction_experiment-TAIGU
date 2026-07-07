using System;
using System.Collections.Generic;

namespace LethalCompanyRegionTag.Cache
{
    /// <summary>
    /// Thread-safe cache for region analysis results.
    /// Entries expire after a configurable TTL to allow re-analysis.
    /// </summary>
    public class RegionCache
    {
        private readonly Dictionary<string, CacheEntry> _cache = new Dictionary<string, CacheEntry>();
        private readonly object _lock = new object();
        private readonly TimeSpan _ttl;

        public RegionCache(TimeSpan? ttl = null)
        {
            _ttl = ttl ?? TimeSpan.FromMinutes(10);
        }

        public bool TryGet(string steamId64, out Analysis.RegionResult result)
        {
            lock (_lock)
            {
                if (_cache.TryGetValue(steamId64, out CacheEntry entry))
                {
                    // Check if expired (but pending results never expire)
                    if (!entry.Result.IsPending && DateTime.UtcNow - entry.CachedAt > _ttl)
                    {
                        _cache.Remove(steamId64);
                        result = null;
                        return false;
                    }

                    result = entry.Result;
                    return true;
                }
            }

            result = null;
            return false;
        }

        public void Set(string steamId64, Analysis.RegionResult result)
        {
            lock (_lock)
            {
                _cache[steamId64] = new CacheEntry
                {
                    Result = result,
                    CachedAt = DateTime.UtcNow
                };
            }
        }

        public void Remove(string steamId64)
        {
            lock (_lock)
            {
                _cache.Remove(steamId64);
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _cache.Clear();
            }
        }

        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _cache.Count;
                }
            }
        }

        /// <summary>
        /// Remove expired entries to free memory.
        /// </summary>
        public void Cleanup()
        {
            lock (_lock)
            {
                var expired = new List<string>();
                var now = DateTime.UtcNow;

                foreach (var kvp in _cache)
                {
                    if (!kvp.Value.Result.IsPending && now - kvp.Value.CachedAt > _ttl)
                        expired.Add(kvp.Key);
                }

                foreach (var key in expired)
                    _cache.Remove(key);
            }
        }

        private class CacheEntry
        {
            public Analysis.RegionResult Result { get; set; }
            public DateTime CachedAt { get; set; }
        }
    }
}
