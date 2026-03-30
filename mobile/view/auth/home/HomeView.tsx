import { useEffect, useState } from "react";
import {
  View,
  Text,
  FlatList,
  StyleSheet,
  TextInput,
  ScrollView,
  TouchableOpacity,
} from "react-native";
import { getMentors } from "../../../viewmodel/home/homeViewModel";

export default function HomeView() {
  const [mentors, setMentors] = useState<any[]>([]);
  const [search, setSearch] = useState("");
  const [selectedSubject, setSelectedSubject] = useState<string | null>(null);

  useEffect(() => {
    load();
  }, []);

  const load = async () => {
    const data = await getMentors();
    setMentors(data);
  };

  const subjects = Array.from(new Set(mentors.map((m) => m.subject)));

  const filtered = mentors.filter((m) => {
    const matchesSearch =
      m.subject.toLowerCase().includes(search.toLowerCase()) ||
      m.name.toLowerCase().includes(search.toLowerCase());
    const matchesSubject = !selectedSubject || m.subject === selectedSubject;
    return matchesSearch && matchesSubject;
  });

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.title}>Student Mentor Hub</Text>
        <Text style={styles.subtitle}>Find your perfect tutor</Text>
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

      {/* Subject Filter */}
      <View style={styles.filterContainer}>
        <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={styles.filterScroll}>
          <TouchableOpacity
            style={[
              styles.filterTag,
              !selectedSubject && styles.filterTagActive,
            ]}
            onPress={() => setSelectedSubject(null)}
          >
            <Text
              style={[
                styles.filterTagText,
                !selectedSubject && styles.filterTagTextActive,
              ]}
            >
              All
            </Text>
          </TouchableOpacity>
          {subjects.map((subject) => (
            <TouchableOpacity
              key={subject}
              style={[
                styles.filterTag,
                selectedSubject === subject && styles.filterTagActive,
              ]}
              onPress={() => setSelectedSubject(subject)}
            >
              <Text
                style={[
                  styles.filterTagText,
                  selectedSubject === subject && styles.filterTagTextActive,
                ]}
              >
                {subject}
              </Text>
            </TouchableOpacity>
          ))}
        </ScrollView>
      </View>

      <FlatList
        data={filtered}
        keyExtractor={(item) => item.id}
        scrollEnabled={false}
        contentContainerStyle={styles.listContent}
        renderItem={({ item }) => (
          <View style={styles.card}>
            <View style={styles.cardHeader}>
              <View style={styles.mentorInfo}>
                <Text style={styles.name}>{item.name}</Text>
                <Text style={styles.subject}>{item.subject}</Text>
              </View>
              <View style={styles.ratingBadge}>
                <Text style={styles.ratingIcon}>⭐</Text>
                <Text style={styles.rating}>{item.rating}</Text>
              </View>
            </View>

            <View style={styles.statsRow}>
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
              <Text style={styles.price}>{item.price}</Text>
              <TouchableOpacity style={styles.bookButton}>
                <Text style={styles.bookButtonText}>Book Now</Text>
              </TouchableOpacity>
            </View>
          </View>
        )}
      />

      {filtered.length === 0 && (
        <View style={styles.emptyState}>
          <Text style={styles.emptyIcon}>🔍</Text>
          <Text style={styles.emptyText}>No mentors found</Text>
          <Text style={styles.emptySubtext}>Try adjusting your search or filters</Text>
        </View>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: "#F8FAFC",
  },
  header: {
    paddingHorizontal: 20,
    paddingTop: 20,
    paddingBottom: 10,
    backgroundColor: "#fff",
    borderBottomWidth: 1,
    borderBottomColor: "#E2E8F0",
  },
  title: {
    fontSize: 28,
    fontWeight: "bold",
    color: "#1E3A8A",
    marginBottom: 4,
  },
  subtitle: {
    fontSize: 14,
    color: "#64748B",
  },
  searchContainer: {
    paddingHorizontal: 20,
    paddingVertical: 12,
    backgroundColor: "#fff",
  },
  search: {
    backgroundColor: "#F1F5F9",
    padding: 12,
    borderRadius: 10,
    fontSize: 14,
    borderWidth: 1,
    borderColor: "#E2E8F0",
    color: "#1E293B",
  },
  filterContainer: {
    paddingVertical: 12,
    paddingHorizontal: 0,
    backgroundColor: "#fff",
    borderBottomWidth: 1,
    borderBottomColor: "#E2E8F0",
  },
  filterScroll: {
    paddingHorizontal: 20,
    paddingRight: 10,
  },
  filterTag: {
    paddingHorizontal: 14,
    paddingVertical: 8,
    borderRadius: 20,
    backgroundColor: "#F1F5F9",
    borderWidth: 1,
    borderColor: "#E2E8F0",
    marginRight: 8,
  },
  filterTagActive: {
    backgroundColor: "#2563EB",
    borderColor: "#2563EB",
  },
  filterTagText: {
    fontSize: 13,
    color: "#64748B",
    fontWeight: "600",
  },
  filterTagTextActive: {
    color: "#fff",
  },
  listContent: {
    paddingHorizontal: 20,
    paddingVertical: 16,
  },
  card: {
    backgroundColor: "#fff",
    padding: 16,
    borderRadius: 12,
    marginBottom: 12,
    shadowColor: "#000",
    shadowOpacity: 0.08,
    shadowRadius: 8,
    elevation: 3,
    borderWidth: 1,
    borderColor: "#E2E8F0",
  },
  cardHeader: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "flex-start",
    marginBottom: 12,
  },
  mentorInfo: {
    flex: 1,
  },
  name: {
    fontSize: 16,
    fontWeight: "bold",
    color: "#1E3A8A",
    marginBottom: 4,
  },
  subject: {
    fontSize: 14,
    color: "#2563EB",
    fontWeight: "600",
  },
  ratingBadge: {
    backgroundColor: "#FEF08A",
    paddingHorizontal: 10,
    paddingVertical: 6,
    borderRadius: 8,
    alignItems: "center",
    flexDirection: "row",
    gap: 4,
  },
  ratingIcon: {
    fontSize: 14,
  },
  rating: {
    fontSize: 13,
    fontWeight: "bold",
    color: "#854D0E",
  },
  statsRow: {
    flexDirection: "row",
    gap: 12,
    marginBottom: 12,
  },
  stat: {
    flex: 1,
    flexDirection: "row",
    alignItems: "center",
    gap: 6,
    backgroundColor: "#F1F5F9",
    paddingHorizontal: 10,
    paddingVertical: 8,
    borderRadius: 8,
  },
  statIcon: {
    fontSize: 16,
  },
  statText: {
    fontSize: 12,
    color: "#64748B",
    fontWeight: "500",
  },
  priceRow: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    borderTopWidth: 1,
    borderTopColor: "#E2E8F0",
    paddingTop: 12,
  },
  price: {
    fontSize: 18,
    fontWeight: "bold",
    color: "#2563EB",
  },
  bookButton: {
    backgroundColor: "#2563EB",
    paddingHorizontal: 20,
    paddingVertical: 8,
    borderRadius: 8,
  },
  bookButtonText: {
    color: "#fff",
    fontWeight: "bold",
    fontSize: 13,
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
  },
});
