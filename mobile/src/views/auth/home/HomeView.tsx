import { useEffect, useState } from "react";
import {
  View,
  Text,
  Image,
  FlatList,
  StyleSheet,
  TextInput,
  TouchableOpacity,
} from "react-native";
import { useRouter } from "expo-router";
import { getMentors } from "../../../viewmodels/home/homeViewModel";
import { getRole, removeToken } from "../../../utils/storage";
import { useMentorReviews } from "../../../contexts/MentorReviewsContext";
import { useMentorChat } from "../../../contexts/MentorChatContext";
import MentorHomeView from "./MentorHomeView";

export default function HomeView() {
  const router = useRouter();
  const { setSelectedMentor } = useMentorReviews();
  const { setSelectedMentorForChat } = useMentorChat();
  const [mentors, setMentors] = useState<any[]>([]);
  const [search, setSearch] = useState("");
  const [selectedSubject, setSelectedSubject] = useState<string | null>(null);
  const [role, setRole] = useState<"student" | "mentor">("student");

  useEffect(() => {
    const initialize = async () => {
      const storedRole = await getRole();
      if (storedRole === "mentor") {
        setRole("mentor");
      }
      const data = await getMentors();
      setMentors(data);
    };

    initialize();
  }, []);

  const subjects = Array.from(new Set(mentors.map((m) => m.subject)));

  const filtered = mentors.filter((m) => {
    const matchesSearch =
      m.subject.toLowerCase().includes(search.toLowerCase()) ||
      m.name.toLowerCase().includes(search.toLowerCase());
    const matchesSubject = !selectedSubject || m.subject === selectedSubject;
    return matchesSearch && matchesSubject;
  });

  const handleLogout = async () => {
    try {
      await removeToken();
    } catch (error) {
      console.error("Logout error:", error);
    } finally {
      router.replace("/auth/login");
    }
  };

  if (role === "mentor") {
    return <MentorHomeView onLogout={handleLogout} onChat={(student) => {
      setSelectedMentorForChat(student);
      router.push("/tabs/chat");
    }} />;
  }

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
              <TouchableOpacity style={styles.logoutButton} onPress={handleLogout}>
                <Text style={styles.logoutButtonText}>Logout</Text>
              </TouchableOpacity>
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
            <Text style={styles.emptyIcon}>🔍</Text>
            <Text style={styles.emptyText}>No mentors found</Text>
            <Text style={styles.emptySubtext}>Try adjusting your search or filters</Text>
          </View>
        }
        renderItem={({ item }) => (
          <View style={styles.card}>
            <View style={styles.cardHeader}>
              <View style={styles.mentorInfoRow}>
                <Image source={{ uri: item.avatar }} style={styles.mentorAvatar} />
                <View style={styles.mentorText}>
                  <Text style={styles.name}>{item.name}</Text>
                  <Text style={styles.subject}>{item.subject}</Text>
                </View>
              </View>
              <View style={styles.headerRight}>
                <View style={[styles.availabilityBadge, item.available ? styles.availableBadge : styles.unavailableBadge]}>
                  <Text style={[styles.availabilityText, item.available ? styles.availableText : styles.unavailableText]}>
                    {item.available ? "Available" : "Busy"}
                  </Text>
                </View>
                <View style={styles.ratingBadge}>
                  <Text style={styles.ratingIcon}>⭐</Text>
                  <Text style={styles.rating}>{item.rating}</Text>
                </View>
              </View>
            </View>

            <View style={styles.statsRowSection}>
              <View style={styles.stat}>
                <Text style={styles.statIcon}>👨‍🎓</Text>
                <Text style={styles.statText}>{item.students} students</Text>
              </View>
              <View style={styles.stat}>
                <Text style={styles.statIcon}>⏱️</Text>
                <Text style={styles.statText}>{item.experience}</Text>
              </View>
            </View>

            <View style={styles.priceRow}>
              <TouchableOpacity
                style={[styles.bookButton, !item.available && styles.bookButtonDisabled]}
                onPress={() => {
                  if (item.available) {
                    setSelectedMentorForChat(item);
                    setSelectedMentor(item); // Also set for reviews
                    router.push("/tabs/chat");
                  }
                }}
                disabled={!item.available}
              >
                <Text style={[styles.bookButtonText, !item.available && styles.bookButtonTextDisabled]}>
                  {item.available ? "Book Now" : "Unavailable"}
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
  mentorInfoRow: {
    flexDirection: "row",
    flex: 1,
    alignItems: "center",
  },
  mentorAvatar: {
    width: 44,
    height: 44,
    borderRadius: 22,
    marginRight: 12,
    backgroundColor: "#E2E8F0",
  },
  mentorText: {
    flex: 1,
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
});
