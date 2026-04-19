import { useEffect, useState } from "react";
import {
  View,
  Text,
  FlatList,
  StyleSheet,
  TextInput,
  TouchableOpacity,
  Alert,
} from "react-native";
import { MaterialIcons } from '@expo/vector-icons';
import { useRouter } from "expo-router";
import eventBus from "../../../utils/eventBus";
import { API_URL } from "../../../constants/api";
import { ensureUserId, getToken } from "../../../utils/storage";
import { useMentorReviews } from "../../../contexts/MentorReviewsContext";
import { useMentorChat } from "../../../contexts/MentorChatContext";

export default function HomeView() {
  const router = useRouter();
  const { setSelectedMentor } = useMentorReviews();
  const { setSelectedMentorForChat } = useMentorChat();
  const [mentors, setMentors] = useState<any[]>([]);
  const [search, setSearch] = useState("");
  const [selectedSubject, setSelectedSubject] = useState<string | null>(null);

  useEffect(() => {
    load();
    const unsub = eventBus.on('reviewUpdated', () => { try { load(); } catch {} });
    const unsub2 = eventBus.on('mentorsUpdated', (payload: any) => { try { setMentors((payload || [])); } catch {} });
    const unsub3 = eventBus.on('mentorResigned', () => { try { load(); } catch {} });
    return () => { 
      if (unsub) unsub(); 
      if (unsub2) unsub2();
      if (unsub3) unsub3();
    };
  }, []);

  const load = async () => {
    try {
      const res = await fetch(`${API_URL}/mentors/list`);
      if (res.ok) {
        const data = await res.json();
        const mapped = data.map((m: any) => {
          const arr = String(m.subjects || m.subject || '').split(',').map((s: string) => s.trim()).filter(Boolean);
          return { ...m, subjectsArray: arr };
        });
        setMentors(mapped);
        return;
      }
    } catch (e) { }
    setMentors([]);
  };

  const subjects = Array.from(
    new Set(
      mentors.flatMap((m) => {
        const sArr = m.subjectsArray || (m.subjects || m.subject || "").toString().split(',').map((x:string)=>x.trim()).filter(Boolean);
        return sArr;
      })
    )
  );

  const filtered = mentors.filter((m) => {
    const subjectVal = (m.subjectsArray || (m.subjects || m.subject || "").toString().split(',').map((x:string)=>x.trim()).filter(Boolean)).join(',');
    const nameVal = String(m.name || "");
    const matchesSearch =
      subjectVal.toLowerCase().includes(search.toLowerCase()) ||
      nameVal.toLowerCase().includes(search.toLowerCase());
    const matchesSubject = !selectedSubject || subjectVal === selectedSubject;
    return matchesSearch && matchesSubject;
  });


  return (
    <View style={styles.container}>
      <FlatList
        data={filtered}
        keyExtractor={(item) => item.id}
        contentContainerStyle={styles.content}
        ListHeaderComponent={
          <>
            <View style={styles.topBar}>
              <View style={styles.topContent}>
                <Text style={styles.greeting}>Welcome back</Text>
                <Text style={styles.greetingSub}>
                  Choose a mentor and get guidance in minutes.
                </Text>
              </View>
              {/* logout moved to Account tab */}
            </View>

            <View style={styles.statsRow}>
              <View style={[styles.statCard, styles.statCardPrimary]}>
                <Text style={styles.statValue}>{mentors.length}</Text>
                <Text style={styles.statLabel}>Mentors available</Text>
              </View>
              <View style={styles.statCard}>
                <Text style={styles.statValue}>95%</Text>
                <Text style={styles.statLabel}>Success rate</Text>
              </View>
            </View>

            <View style={styles.searchContainer}>
              <TextInput
                placeholder="Search mentors or subjects..."
                value={search}
                onChangeText={setSearch}
                style={styles.search}
                placeholderTextColor="#94A3B8"
              />
            </View>

            <View style={styles.filterContainer}>
              <FlatList
                horizontal
                data={["All", ...subjects]}
                keyExtractor={(item) => item}
                showsHorizontalScrollIndicator={false}
                contentContainerStyle={styles.filterScroll}
                renderItem={({ item }) => {
                  const isActive = item === "All" ? !selectedSubject : selectedSubject === item;
                  return (
                    <TouchableOpacity
                      style={[styles.filterTag, isActive && styles.filterTagActive]}
                      onPress={() => setSelectedSubject(item === "All" ? null : item)}
                    >
                      <Text style={[styles.filterTagText, isActive && styles.filterTagTextActive]}>
                        {item}
                      </Text>
                    </TouchableOpacity>
                  );
                }}
              />
            </View>
          </>
        }
        ListEmptyComponent={
          <View style={styles.emptyState}>
            <MaterialIcons name="search" size={48} color="#94A3B8" />
            <Text style={styles.emptyText}>No mentors found</Text>
            <Text style={styles.emptySubtext}>Try adjusting your search or filters</Text>
          </View>
        }
        renderItem={({ item }) => (
          <View style={styles.card}>
            <View style={styles.cardHeader}>
              <View style={{ flexDirection: 'row', alignItems: 'center', flex: 1 }}>
                <View style={styles.avatar}>
                  <Text style={styles.avatarText}>
                    {(String(item.name || 'U').trim().split(' ')[0]?.charAt(0) || 'U').toUpperCase()}
                  </Text>
                </View>
                <View style={styles.mentorInfo}>
                  <Text style={styles.name}>{item.name}</Text>
                  <View style={styles.subjectsRow}>
                    {(item.subjectsArray || []).slice(0, 4).map((s: string) => (
                      <Text key={s} style={styles.subjectTag}>{s}</Text>
                    ))}
                  </View>
                </View>
              </View>
              <View style={styles.headerRight}>
                  {(() => {
                    const canChat = Boolean(item.userId || item.id);
                    return (
                      <View style={[styles.availabilityBadge, canChat ? styles.availableBadge : styles.unavailableBadge]}>
                        <Text style={[styles.availabilityText, canChat ? styles.availableText : styles.unavailableText]}>
                          {canChat ? "Available" : "Busy"}
                        </Text>
                      </View>
                    );
                  })()}
                <View style={styles.ratingBadge}>
                  <MaterialIcons name="star" size={14} color="#F59E0B" />
                  <Text style={styles.rating}>{item.rating ? (Math.round((item.rating) * 10) / 10).toFixed(1) : "-"}</Text>
                </View>
              </View>
            </View>

            <View style={styles.statsRowSection}>
              <View style={styles.stat}>
                <MaterialIcons name="school" size={16} color="#2563EB" style={{ marginRight: 8 }} />
                <View>
                  <Text style={styles.statText}>{item.students} students</Text>
                </View>
              </View>
              <View style={styles.stat}>
                <MaterialIcons name="access-time" size={16} color="#64748B" style={{ marginRight: 8 }} />
                <View style={styles.statRightColumn}>
                  {item.createdAt ? (
                    <Text style={[styles.statText, styles.statDateCompact]}>{new Date(item.createdAt).toLocaleDateString()}</Text>
                  ) : null}
                </View>
              </View>
            </View>

            <View style={styles.priceRow}>
              <TouchableOpacity
                style={[styles.bookButton, !(item.userId || item.id) && styles.bookButtonDisabled]}
                onPress={() => {
                  (async () => {
                    try {
                      const token = await getToken();
                      const uid = await ensureUserId();

                      let myRealId: string | null = null;
                      if (token) {
                        try {
                          const rMe = await fetch(`${API_URL}/auth/me`, { headers: { Authorization: `Bearer ${token}` } });
                          if (rMe.ok) {
                            const me = await rMe.json();
                            myRealId = String(me.id);
                          }
                        } catch { }
                      }

                      const targetId = String(item.userId || item.id || '');

                      if (targetId && (targetId === myRealId || targetId === uid)) {
                        Alert.alert(
                          "Invalid action",
                          "You cannot chat with yourself."
                        );
                        return;
                      }

                      const res = await fetch(`${API_URL}/chats`, {
                        method: 'POST', headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}) },
                          body: JSON.stringify({ mentorId: targetId, senderId: uid })
                      });
                      if (!res.ok) {
                        console.warn('Start chat failed', res.status, await res.text());
                      } else {
                        try { setSelectedMentorForChat(item); } catch {}
                        setSelectedMentor(item);
                        setMentors(prev => prev.map(m => m.id === item.id ? { ...m, students: (m.students || 0) + 1 } : m));
                        try { eventBus.emit('mentorUpdated', { mentorId: item.id }); } catch {}
                        router.push('/tabs/chat');
                      }
                    } catch (e) { console.warn('Start chat error', e); }
                  })();
                }}
                >
                <Text style={[styles.bookButtonText, !(item.userId || item.id) && styles.bookButtonTextDisabled]}>
                  Start Chat
                </Text>
              </TouchableOpacity>
              <TouchableOpacity
                style={styles.reviewsButton}
                onPress={() => {
                  setSelectedMentor(item);
                  router.push("/tabs/reviews");
                }}
              >
                <Text style={styles.reviewsButtonText}>View Reviews</Text>
              </TouchableOpacity>
            </View>
          </View>
        )}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: "#EEF2FF",
  },
  content: {
    paddingTop: 48,
    paddingBottom: 24,
  },
  topBar: {
    marginHorizontal: 20,
    marginTop: 18,
    marginBottom: 18,
    padding: 22,
    borderRadius: 24,
    backgroundColor: "#2563EB",
    shadowColor: "#000",
    shadowOpacity: 0.12,
    shadowRadius: 18,
    shadowOffset: { width: 0, height: 10 },
    elevation: 8,
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "flex-start",
  },
  topContent: {
    flex: 1,
    marginRight: 12,
  },
  greeting: {
    color: "#FFFFFF",
    fontSize: 28,
    fontWeight: "bold",
    marginBottom: 8,
  },
  greetingSub: {
    color: "#DBEAFE",
    fontSize: 15,
    lineHeight: 22,
  },
  logoutButton: {
    alignSelf: "flex-start",
    backgroundColor: "rgba(255,255,255,0.18)",
    paddingHorizontal: 16,
    paddingVertical: 10,
    borderRadius: 18,
  },
  logoutButtonText: {
    color: "#FFFFFF",
    fontWeight: "700",
    fontSize: 13,
  },
  statsRow: {
    flexDirection: "row",
    justifyContent: "space-between",
    paddingHorizontal: 20,
    marginBottom: 18,
  },
  statCard: {
    flex: 1,
    backgroundColor: "#ffffff",
    borderRadius: 18,
    padding: 16,
    marginRight: 12,
    shadowColor: "#000",
    shadowOpacity: 0.05,
    shadowRadius: 10,
    elevation: 3,
  },
  statCardPrimary: {
    backgroundColor: "#2563EB",
  },
  statValue: {
    fontSize: 24,
    fontWeight: "bold",
    color: "#1E293B",
    marginBottom: 4,
  },
  statLabel: {
    fontSize: 12,
    color: "#64748B",
    fontWeight: "600",
  },
  searchContainer: {
    marginHorizontal: 20,
    marginBottom: 14,
  },
  search: {
    backgroundColor: "#FFFFFF",
    borderRadius: 18,
    paddingVertical: 14,
    paddingHorizontal: 18,
    fontSize: 15,
    borderWidth: 1,
    borderColor: "#E2E8F0",
    color: "#0F172A",
    shadowColor: "#000",
    shadowOpacity: 0.03,
    shadowRadius: 10,
    elevation: 2,
  },
  filterContainer: {
    marginBottom: 10,
  },
  filterScroll: {
    paddingLeft: 20,
    paddingRight: 10,
  },
  filterTag: {
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderRadius: 20,
    backgroundColor: "#FFFFFF",
    borderWidth: 1,
    borderColor: "#E2E8F0",
    marginRight: 10,
  },
  filterTagActive: {
    backgroundColor: "#2563EB",
    borderColor: "#2563EB",
  },
  filterTagText: {
    fontSize: 13,
    color: "#475569",
    fontWeight: "600",
  },
  filterTagTextActive: {
    color: "#FFFFFF",
  },
  card: {
    backgroundColor: "#FFFFFF",
    padding: 20,
    borderRadius: 24,
    marginHorizontal: 20,
    marginBottom: 16,
    shadowColor: "#000",
    shadowOpacity: 0.06,
    shadowRadius: 14,
    shadowOffset: { width: 0, height: 6 },
    elevation: 4,
    borderWidth: 1,
    borderColor: "#E2E8F0",
  },
  cardHeader: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "flex-start",
    marginBottom: 16,
  },
  mentorInfo: {
    flex: 1,
  },
  name: {
    fontSize: 18,
    fontWeight: "800",
    color: "#0F172A",
    marginBottom: 6,
  },
  subject: {
    fontSize: 14,
    color: "#2563EB",
    fontWeight: "700",
  },
  subjectsRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    marginTop: 4,
  },
  subjectTag: {
    fontSize: 12,
    color: '#2563EB',
    backgroundColor: 'rgba(37,99,235,0.08)',
    paddingHorizontal: 8,
    paddingVertical: 4,
    borderRadius: 10,
    marginRight: 6,
    marginBottom: 6,
    fontWeight: '700',
  },
  headerRight: {
    alignItems: "flex-end",
    gap: 8,
  },
  availabilityBadge: {
    paddingHorizontal: 10,
    paddingVertical: 6,
    borderRadius: 14,
    alignSelf: "flex-start",
  },
  availableBadge: {
    backgroundColor: "#DCFCE7",
  },
  unavailableBadge: {
    backgroundColor: "#FEF3C7",
  },
  availabilityText: {
    fontSize: 12,
    fontWeight: "700",
  },
  availableText: {
    color: "#166534",
  },
  unavailableText: {
    color: "#92400E",
  },
  ratingBadge: {
    backgroundColor: "#FCE7A9",
    paddingHorizontal: 10,
    paddingVertical: 8,
    borderRadius: 16,
    alignItems: "center",
    flexDirection: "row",
  },
  ratingIcon: {
    fontSize: 14,
  },
  rating: {
    fontSize: 13,
    fontWeight: "800",
    color: "#92400E",
    marginLeft: 6,
  },
  statDate: {
    fontSize: 11,
    color: '#64748B',
    marginTop: 2,
  },
  statsRowSection: {
    flexDirection: "row",
    marginBottom: 16,
  },
  stat: {
    flex: 1,
    flexDirection: "row",
    alignItems: "center",
    marginRight: 12,
    backgroundColor: "#F8FAFC",
    paddingHorizontal: 12,
    paddingVertical: 10,
    borderRadius: 14,
  },
  statIcon: {
    fontSize: 16,
    marginRight: 8,
  },
  statText: {
    fontSize: 12,
    color: "#64748B",
    fontWeight: "600",
  },
  statRightColumn: {
    justifyContent: 'center',
    alignItems: 'center',
  },
  statDateCompact: {
    marginTop: 0,
    textAlign: 'center',
  },
  priceRow: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
  },
  bookButton: {
    backgroundColor: "#2563EB",
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderRadius: 16,
    flex: 1,
    marginRight: 8,
  },
  bookButtonDisabled: {
    backgroundColor: "#E5E7EB",
  },
  bookButtonText: {
    color: "#FFFFFF",
    fontWeight: "800",
    fontSize: 13,
    textAlign: "center",
  },
  bookButtonTextDisabled: {
    color: "#9CA3AF",
  },
  reviewsButton: {
    backgroundColor: "#F1F5F9",
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderRadius: 16,
    borderWidth: 1,
    borderColor: "#E2E8F0",
    flex: 1,
    marginLeft: 8,
  },
  reviewsButtonText: {
    color: "#475569",
    fontWeight: "700",
    fontSize: 13,
    textAlign: "center",
  },
  emptyState: {
    alignItems: "center",
    justifyContent: "center",
    paddingVertical: 60,
  },
  emptyIcon: {
    fontSize: 48,
    marginBottom: 16,
  },
  emptyText: {
    fontSize: 18,
    fontWeight: "bold",
    color: "#1E3A8A",
    marginBottom: 8,
  },
  emptySubtext: {
    fontSize: 14,
    color: "#64748B",
    textAlign: "center",
  },
  avatar: {
    width: 48,
    height: 48,
    borderRadius: 24,
    backgroundColor: "#F3F4F6",
    marginRight: 12,
    alignItems: 'center',
    justifyContent: 'center',
  },
  avatarText: {
    color: '#1D4ED8',
    fontWeight: '700',
    fontSize: 18,
  },
});
