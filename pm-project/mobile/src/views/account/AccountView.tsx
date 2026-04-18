import { useEffect, useState } from "react";
import { View, Text, ScrollView, StyleSheet, TouchableOpacity, TextInput, Alert } from "react-native";
import AsyncStorage from '@react-native-async-storage/async-storage';
import { MaterialIcons } from '@expo/vector-icons';
import { API_URL } from "../../constants/api";
import { getMentors } from "../../viewmodels/home/homeViewModel";
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
  const [isMentor, setIsMentor] = useState(false);
  const [showThank, setShowThank] = useState(false);
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
            const mentorFlag = j.isMentor === true;
            setIsMentor(mentorFlag);
            setShowThank(mentorFlag);
            if (!uid && j.id) setUserId(j.id);
          }
        }

        const authoredCacheKey = `authored`;
        const authoredCached = reviewCache.get(authoredCacheKey);
        if (authoredCached) setWritten(authoredCached);

        if (token) {
          const r2 = await fetch(`${API_URL}/reviews/authored`, { headers: { Authorization: `Bearer ${token}` } });
          if (r2.ok) {
            const data = await r2.json();
            const resolved = await Promise.all(data.map(async (item: any) => {
              if (!item.reviewedUserName && item.reviewedExternalId) {
                try {
                  const resp = await fetch(`${API_URL}/mentors/resolve/${item.reviewedExternalId}`);
                  if (resp.ok) {
                    const jr = await resp.json();
                    item.reviewedUserName = jr.displayName ?? null;
                  }
                } catch (e) { /* ignore */ }
                if (!item.reviewedUserName) {
                  try {
                    const local = await getMentors();
                    const found = local.find((m: any) => String(m.id) === String(item.reviewedExternalId));
                    if (found) item.reviewedUserName = found.name;
                  } catch (e) { }
                }
              }
              return item;
            }));
              setWritten(resolved);
              reviewCache.set(authoredCacheKey, resolved);
            setFetchAuthError(null);
          } else {
            setFetchAuthError(`Failed to load authored reviews: ${r2.status}`);
          }
        } else {
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

        if (uid) {
          const r3 = await fetch(`${API_URL}/reviews/${uid}`);
          if (r3.ok) setAbout(await r3.json());
        }
      } catch (err) {
        console.warn("Account load error", err);
      }
    })();

    const unsubMentor = eventBus.on('mentorCreated', (payload: any) => {
      try {
        if (!payload) return;
        const createdUserId = payload.userId || payload.userId === 0 ? String(payload.userId) : null;
        if (createdUserId && String(createdUserId) === String(userId)) {
          setIsMentor(true);
          setShowThank(true);
          (async () => {
            const token = await getToken();
            if (!token) return;
            const r2 = await fetch(`${API_URL}/reviews/authored`, { headers: { Authorization: `Bearer ${token}` } });
            if (r2.ok) {
              const data = await r2.json();
              const resolved = await Promise.all(data.map(async (item: any) => {
                if (!item.reviewedUserName && item.reviewedExternalId) {
                  try {
                    const resp = await fetch(`${API_URL}/mentors/resolve/${item.reviewedExternalId}`);
                    if (resp.ok) {
                      const jr = await resp.json();
                      item.reviewedUserName = jr.displayName ?? null;
                    }
                    if (!item.reviewedUserName) {
                      try {
                        const local = await getMentors();
                        const found = local.find((m: any) => String(m.id) === String(item.reviewedExternalId));
                        if (found) item.reviewedUserName = found.name;
                      } catch (e) { }
                    }
                  } catch (e) { /* ignore */ }
                }
                return item;
              }));
              setWritten(resolved);
              reviewCache.set('authored', resolved);
            }
          })();
        }
      } catch (e) { }
    });

    const unsubReview = eventBus.on('reviewUpdated', async (payload: any) => {
      try {
        const token = await getToken();
        const uid = await getUserId();

        if (token) {
          const r2 = await fetch(`${API_URL}/reviews/authored`, { headers: { Authorization: `Bearer ${token}` } });
          if (r2.ok) {
            const data = await r2.json();
            const resolved = await Promise.all(data.map(async (item: any) => {
              if (!item.reviewedUserName && item.reviewedExternalId) {
                try {
                  const resp = await fetch(`${API_URL}/mentors/resolve/${item.reviewedExternalId}`);
                  if (resp.ok) {
                    const jr = await resp.json();
                    item.reviewedUserName = jr.displayName ?? null;
                  }
                } catch (e) { }
                if (!item.reviewedUserName) {
                  try {
                    const local = await getMentors();
                    const found = local.find((m: any) => String(m.id) === String(item.reviewedExternalId));
                    if (found) item.reviewedUserName = found.name;
                  } catch (e) { }
                }
              }
              return item;
            }));
            setWritten(resolved);
            reviewCache.set('authored', resolved);
          }
        }

        if (uid) {
          const r3 = await fetch(`${API_URL}/reviews/${uid}`);
          if (r3.ok) setAbout(await r3.json());
        }
      } catch (e) { }
    });

    return () => { if (unsubMentor) unsubMentor(); if (unsubReview) unsubReview(); };
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

  const handleResign = async () => {
    Alert.alert(
      "Stop being a mentor",
      "Are you sure you want to stop being a mentor? This will remove your mentor profile and subjects.",
      [
        { text: 'Cancel', style: 'cancel' },
        { text: 'Yes, stop', style: 'destructive', onPress: async () => {
          try {
            const token = await getToken();
            if (!token) return Alert.alert('Not signed in');
            const res = await fetch(`${API_URL}/mentors/resign`, { method: 'POST', headers: { Authorization: `Bearer ${token}` } });
            if (!res.ok) {
              const text = await res.text();
              console.warn('Resign failed', res.status, text);
              return Alert.alert('Failed', 'Could not stop being a mentor');
            }
            setIsMentor(false);
            setShowThank(false);
            try {
              if (userId) await AsyncStorage.removeItem(`mentor_thank_shown:${userId}`);
            } catch (e) { }

            try {
              const token2 = await getToken();
              if (token2) {
                const rChats = await fetch(`${API_URL}/chats/mine`, { headers: { Authorization: `Bearer ${token2}` } });
                if (rChats.ok) {
                  const chats = await rChats.json();
                  try { eventBus.emit('chatsUpdated', chats); } catch {}
                }
              }
              const rMentors = await fetch(`${API_URL}/mentors/list`);
              if (rMentors.ok) {
                const mentors = await rMentors.json();
                try { eventBus.emit('mentorsUpdated', mentors); } catch {}
              }
            } catch (e) { }
            try { eventBus.emit('mentorResigned', { userId }); } catch {};
          } catch (err) {
            console.warn(err);
            Alert.alert('Error', 'An error occurred');
          }
        }}
      ]
    );
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
          {!isMentor ? (
            <View style={styles.mentorPromo}>
              <Text style={styles.mentorPromoTitle}>Share your knowledge — become a mentor</Text>
              <Text style={styles.mentorPromoText}>Help learners, get reviews, and open a chat for mentees to reach you.</Text>
              <View style={{ flexDirection: 'row', marginTop: 12 }}>
                <TouchableOpacity style={styles.mentorButton} onPress={() => router.push('/mentor/create')}>
                  <Text style={{ color: '#fff', fontWeight: '700' }}>Become a mentor</Text>
                </TouchableOpacity>
              </View>
            </View>
          ) : (
            <View style={styles.mentorPromoActive}>
              {showThank ? (
                <>
                  <Text style={styles.mentorPromoTitle}>Thank you for sharing your knowledge</Text>
                  <Text style={styles.mentorPromoText}>Your mentor profile is active — learners can message and review you.</Text>
                </>
              ) : (
                <>
                  <Text style={styles.mentorPromoTitle}>Your mentor profile is active</Text>
                  <Text style={styles.mentorPromoText}>Learners can message and review you.</Text>
                </>
              )}
              <View style={{ flexDirection: 'row', marginTop: 12 }}>
                <TouchableOpacity style={styles.mentorButton} onPress={() => router.push('/mentor/create')}>
                  <Text style={{ color: '#fff', fontWeight: '700' }}>Change subjects</Text>
                </TouchableOpacity>
                <TouchableOpacity style={[styles.mentorButton, { backgroundColor: '#EF4444', marginLeft: 10 }]} onPress={handleResign}>
                  <Text style={{ color: '#fff', fontWeight: '700' }}>Stop being a mentor</Text>
                </TouchableOpacity>
                
              </View>
            </View>
          )}
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
                  <MaterialIcons key={i} name={i < r.rating ? 'star' : 'star-border'} size={16} color="#FACC15" style={{ marginRight: 4 }} />
                ))}
              </View>
              {editingId === r.id ? (
                <>
                  <View style={{ flexDirection: 'row', marginBottom: 8 }}>
                    {[1,2,3,4,5].map((v) => (
                      <TouchableOpacity key={v} onPress={() => setEditRating(v)} style={{ marginRight: 8 }}>
                        <MaterialIcons name={editRating >= v ? 'star' : 'star-border'} size={20} color={editRating >= v ? '#FACC15' : '#CBD5E1'} />
                      </TouchableOpacity>
                    ))}
                  </View>
                  <TextInput value={editContent} onChangeText={setEditContent} style={{ backgroundColor: '#F8FAFC', borderRadius: 8, padding: 8, marginBottom: 8 }} multiline />
                  <View style={{ flexDirection: 'row' }}>
                    <TouchableOpacity style={[styles.saveButton, { marginRight: 8 }]} onPress={async () => {
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
    mentorPromo: {
      backgroundColor: '#fff',
      borderRadius: 16,
      padding: 16,
      marginBottom: 12,
    },
    mentorPromoActive: {
      backgroundColor: '#fff',
      borderRadius: 16,
      padding: 16,
      marginBottom: 12,
    },
    mentorPromoTitle: { fontSize: 16, fontWeight: '700', color: '#0F172A' },
    mentorPromoText: { color: '#64748B', marginTop: 6 },
    mentorButton: { backgroundColor: '#2563EB', paddingVertical: 10, paddingHorizontal: 14, borderRadius: 12 },
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
