type CacheEntry = { ts: number; data: any };

const cache = new Map<string, CacheEntry>();
const TTL = 60 * 1000; // 60s

export default {
  get(key: string) {
    const e = cache.get(key);
    if (!e) return null;
    if (Date.now() - e.ts > TTL) {
      cache.delete(key);
      return null;
    }
    return e.data;
  },
  set(key: string, data: any) {
    cache.set(key, { ts: Date.now(), data });
  },
  clear(key?: string) {
    if (key) cache.delete(key);
    else cache.clear();
  }
};
