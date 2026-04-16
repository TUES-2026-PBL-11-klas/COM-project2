import { useEffect, useState } from "react";
import { View, Text, ScrollView, StyleSheet, TouchableOpacity, TextInput } from "react-native";
import { API_URL } from "../../constants/api";
import { getToken, getUserId, removeToken } from "../../utils/storage";
import reviewCache from "../../utils/reviewCache";
import eventBus from "../../utils/eventBus";
import { useRouter } from "expo-router";

interface OutReview {
  id: string;
  reviewerId?: string;
  reviewerName?: string;
  reviewedUserId?: string | null;
  reviewedExternalId?: string | null;
  reviewedUserName?: string | null;
  rating: number;
  content: string;
  createdAt: string;
}

export default function AccountView() {
  const [userId, setUserId] = useState<string | null>(null);
  const [username, setUsername] = useState<string | null>(null);
  const [written, setWritten] = useState<OutReview[]>([]);
  const [about, setAbout] = useState<OutReview[]>([]);
  const [hasToken, setHasToken] = useState(false);
  const [fetchAuthError, setFetchAuthError] = useState<string | null>(null);
  const router = useRouter();
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editRating, setEditRating] = useState<number>(5);
  const [editContent, setEditContent] = useState<string>("");

  useEffect(() => {
    (async () => {
      try {
        const token = await getToken();
        const uid = await getUserId();
        setUserId(uid);
        setHasToken(!!token);

        if (token) {
          const r = await fetch(`${API_URL}/auth/me`, { headers: { Authorization: `Bearer ${token}` } });
          if (r.ok) {
            const j = await r.json();
            setUsername(j.username || null);
            if (!uid && j.id) setUserId(j.id);
          }
        }

        // fetch reviews I've written (authored)
        const authoredCacheKey = `authored`;
        const authoredCached = reviewCache.get(authoredCacheKey);
        if (authoredCached) setWritten(authoredCached);

        if (token) {
          const r2 = await fetch(`${API_URL}/reviews/authored`, { headers: { Authorization: `Bearer ${token}` } });
          if (r2.ok) {
            const data = await r2.json();
            setWritten(data);
            reviewCache.set(authoredCacheKey, data);
            setFetchAuthError(null);
          } else {
            setFetchAuthError(`Failed to load authored reviews: ${r2.status}`);
          }
        } else {
          // try a permissive fetch in case server allows authored lookup without auth
          try {
            const r2 = await fetch(`${API_URL}/reviews/authored`);
            if (r2.ok) {
              const data = await r2.json();
              setWritten(data);
              reviewCache.set(authoredCacheKey, data);
            } else setFetchAuthError(`Failed to load authored reviews: ${r2.status}`);
          } catch (err) {
            setFetchAuthError('Network error fetching authored reviews');
          }
        }

        // fetch reviews about me (if we have an id)
        if (uid) {
          const r3 = await fetch(`${API_URL}/reviews/${uid}`);
          if (r3.ok) setAbout(await r3.json());
        }
      } catch (err) {
        console.warn("Account load error", err);
      }
    })();
  }, []);

  const handleLogout = async () => {
    try {
      await removeToken();
    } catch (err) {
      console.warn("Logout error", err);
    } finally {
      router.replace("/auth/login");
    }
  };

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      <View style={styles.headerCard}>
        <Text style={styles.name}>Welcome Back, {username || "You"}</Text>
        <Text style={styles.sub}>How are you doing today?</Text>
        <TouchableOpacity style={styles.logoutButton} onPress={handleLogout}>
          <Text style={styles.logoutText}>Logout</Text>
        </TouchableOpacity>
      </View>

      <View style={styles.section}>
        <Text style={styles.sectionTitle}>Reviews I Wrote</Text>
        {written.length === 0 ? (
          fetchAuthError ? (
            <Text style={styles.empty}>{fetchAuthError} — try signing in.</Text>
          ) : !hasToken ? (
            <Text style={styles.empty}>Sign in to see reviews you've written.</Text>
          ) : (
            <Text style={styles.empty}>You haven't written any reviews yet.</Text>
          )
        ) : (
          written.map((r) => (
            <View key={r.id} style={styles.reviewCard}>
              <Text style={styles.reviewMeta}>To: {r.reviewedUserName || r.reviewedExternalId || (r.reviewedUserId ? String(r.reviewedUserId).slice(0,8) : "Unknown")} • {new Date(r.createdAt).toLocaleDateString()}</Text>
              <View style={{ flexDirection: 'row', marginBottom: 8 }}>
                {Array.from({ length: 5 }).map((_, i) => (
                  <Text key={i} style={{ color: '#FACC15', marginRight: 4 }}>{i < r.rating ? '★' : '☆'}</Text>
                ))}
              </View>
              {editingId === r.id ? (
                <>
                  <View style={{ flexDirection: 'row', marginBottom: 8 }}>
                    {[1,2,3,4,5].map((v) => (
                      <TouchableOpacity key={v} onPress={() => setEditRating(v)} style={{ marginRight: 8 }}>
                        <Text style={{ fontSize: 20, color: editRating >= v ? '#FACC15' : '#CBD5E1' }}>{editRating >= v ? '★' : '☆'}</Text>
                      </TouchableOpacity>
                    ))}
                  </View>
                  <TextInput value={editContent} onChangeText={setEditContent} style={{ backgroundColor: '#F8FAFC', borderRadius: 8, padding: 8, marginBottom: 8 }} multiline />
                  <View style={{ flexDirection: 'row' }}>
                    <TouchableOpacity style={[styles.saveButton, { marginRight: 8 }]} onPress={async () => {
                        // save
                        try {
                          const token = await getToken();
                          if (!token) {
                            console.warn('Not authenticated');
                            return;
                          }
                          const body = { reviewedId: r.reviewedExternalId || (r.reviewedUserId || ''), rating: editRating, content: editContent };
                          const res = await fetch(`${API_URL}/reviews/${r.id}`, {
                            method: 'PUT', headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
                            body: JSON.stringify(body)
                          });
                          if (!res.ok) {
                            console.warn('Failed to save review', res.status, await res.text());
                            return;
                          }
                          const updated = await res.json();
                          setWritten(prev => prev.map(p => p.id === r.id ? { ...p, rating: updated.rating, content: updated.content } : p));
                          // notify other views (e.g., ReviewsView) to refresh
                          try { eventBus.emit('reviewUpdated', { reviewedExternalId: updated.reviewedExternalId, reviewedUserId: updated.reviewedUserId }); } catch {}
                          setEditingId(null);
                        } catch (err) { console.warn(err); }
                      }}>
                        <Text style={styles.logoutText}>Save</Text>
                      </TouchableOpacity>
                    <TouchableOpacity style={[styles.logoutButton, { backgroundColor: '#94A3B8' }]} onPress={() => setEditingId(null)}>
                      <Text style={styles.logoutText}>Cancel</Text>
                    </TouchableOpacity>
                  </View>
                </>
              ) : (
                <>
                  <Text style={styles.reviewContent}>{r.content}</Text>
                  <View style={{ flexDirection: 'row', marginTop: 8 }}>
                    <TouchableOpacity style={{ marginRight: 12 }} onPress={() => { setEditingId(r.id); setEditRating(r.rating); setEditContent(r.content); }}>
                      <Text style={{ color: '#2563EB', fontWeight: '700' }}>Edit</Text>
                    </TouchableOpacity>
                    <TouchableOpacity onPress={async () => {
                      try {
                        const token = await getToken();
                        const res = await fetch(`${API_URL}/reviews/${r.id}`, { method: 'DELETE', headers: { Authorization: `Bearer ${token}` } });
                        if (res.status === 204) setWritten(prev => prev.filter(p => p.id !== r.id));
                          // notify other views to refresh
                          try { eventBus.emit('reviewUpdated', { reviewedExternalId: r.reviewedExternalId, reviewedUserId: r.reviewedUserId }); } catch {}
                      } catch (err) { console.warn(err); }
                    }}>
                      <Text style={{ color: '#EF4444', fontWeight: '700' }}>Delete</Text>
                    </TouchableOpacity>
                  </View>
                </>
              )}
            </View>
          ))
        )}
      </View>

      <View style={styles.section}>
        <Text style={styles.sectionTitle}>Reviews About Me</Text>
        {about.length === 0 ? (
          <Text style={styles.empty}>No one has reviewed you yet.</Text>
        ) : (
          about.map((r) => (
            <View key={r.id} style={styles.reviewCard}>
              <Text style={styles.reviewMeta}>{r.reviewerName || "Anonymous"} • {new Date(r.createdAt).toLocaleDateString()}</Text>
              <Text style={styles.reviewContent}>{r.content}</Text>
            </View>
          ))
        )}
      </View>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: "#EEF2FF" },
  content: { padding: 16, paddingTop: 48, paddingBottom: 40 },
  headerCard: {
    backgroundColor: "#2563EB",
    borderRadius: 18,
    padding: 20,
    paddingTop: 36,
    paddingBottom: 28,
    alignItems: "flex-start",
    minHeight: 120,
    justifyContent: 'flex-start',
    marginBottom: 18,
    position: 'relative',
  },
  emoji: { fontSize: 48, marginBottom: 8 },
  name: { fontSize: 28, fontWeight: "700", color: "#FFFFFF", textAlign: 'left' },
  sub: { color: "#DBEAFE", marginTop: 6, fontSize: 15 },
  section: { marginTop: 8 },
  sectionTitle: { fontSize: 16, fontWeight: "700", marginBottom: 8 },
  empty: { color: "#64748B", fontStyle: "italic" },
  reviewCard: { backgroundColor: "#fff", padding: 14, borderRadius: 12, marginBottom: 10 },
  reviewMeta: { color: "#94A3B8", marginBottom: 6 },
  reviewContent: { color: "#0F172A" },
  logoutButton: {
    position: 'absolute',
    right: 16,
    bottom: 16,
    alignSelf: 'flex-end',
    backgroundColor: 'rgba(255,255,255,0.18)',
    paddingHorizontal: 16,
    paddingVertical: 10,
    borderRadius: 18,
  },
  logoutText: { color: "#FFFFFF", fontWeight: "700", fontSize: 13 },
  saveButton: { backgroundColor: '#2563EB', paddingVertical: 10, paddingHorizontal: 14, borderRadius: 12 },
});
