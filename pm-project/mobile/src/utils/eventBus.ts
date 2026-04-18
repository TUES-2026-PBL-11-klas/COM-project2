type Callback = (payload?: any) => void;

const listeners: Record<string, Callback[]> = {};

export default {
  on(event: string, cb: Callback) {
    listeners[event] = listeners[event] || [];
    listeners[event].push(cb);
    return () => { listeners[event] = listeners[event].filter(c => c !== cb); };
  },
  off(event: string, cb: Callback) {
    if (!listeners[event]) return;
    listeners[event] = listeners[event].filter(c => c !== cb);
  },
  emit(event: string, payload?: any) {
    (listeners[event] || []).slice().forEach(cb => cb(payload));
  }
};
