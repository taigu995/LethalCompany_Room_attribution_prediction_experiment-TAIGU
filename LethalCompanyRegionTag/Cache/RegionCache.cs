using System;
using System.Collections.Generic;

namespace LethalCompanyRegionTag.Cache
{
    /// <summary>
    /// Thread-safe cache for region analysis results.
    /// Uses Steam ID (ulong) as key with TTL-based expiration.
    /// </summary>
    public class RegionCache
    {
        private static readonly Dictionary<ulong, CacheEntry> _cache = new Dictionary<ulong, CacheEntry>();
        private static readonly object _lock = new object();
        private static readonly TimeSpan DefaultTTL = TimeSpan.FromMinutes(10);

        private struct CacheEntry
        {
            public Analysis.RegionResult Result;
            public DateTime ExpiresAt;
        }

        /// <summary>
        /// Try to get a cached result for the given Steam ID.
        /// </summary>
        public static bool TryGet(ulong steamId, out Analysis.RegionResult result)
        {
            lock (_lock)
            {
                if (_cache.TryGetValue(steamId, out var entry))
                {
                    if (DateTime.UtcNow < entry.ExpiresAt)
                    {
                        result = entry.Result;
                        return true;
                    }
                    // Expired, remove it
                    _cache.Remove(steamId);
                }
            }
            result = null;
            return false;
        }

        /// <summary>
        /// Set a cached result for the given Steam ID.
        /// </summary>
        public static void Set(ulong steamId, Analysis.RegionResult result)
        {
            if (steamId == 0 || result == null) return;

            lock (_lock)
            {
                _cache[steamId] = new CacheEntry
                {
                    Result = result,
                    ExpiresAt = DateTime.UtcNow + DefaultTTL
                };
            }
        }

        /// <summary>
        /// Clear all cached results.
        /// </summary>
        public static void Clear()
        {
            lock (_lock)
            {
                _cache.Clear();
            }
        }

        /// <summary>
        /// Remove expired entries to free memory.
        /// </summary>
        public static void Cleanup()
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                var expired = new List<ulong>();
                foreach (var kvp in _cache)
                {
                    if (now >= kvp.Value.ExpiresAt)
                        expired.Add(kvp.Key);
                }
                foreach (var key in expired)
                    _cache.Remove(key);
            }
        }
    }
}
