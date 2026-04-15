import { useState } from "react";
import {
  View,
  Text,
  ScrollView,
  TextInput,
  TouchableOpacity,
  StyleSheet,
} from "react-native";

interface Review {
  id: string;
  name: string;
  rating: number;
  comment: string;
  date: string;
}

interface Mentor {
  id: string;
  name: string;
  subject: string;
  rating: number;
  reviews: Review[];
}

interface ReviewsViewProps {
  mentor?: Mentor | null;
}

export default function ReviewsView({ mentor }: ReviewsViewProps) {
  const [reviews, setReviews] = useState<Review[]>(mentor?.reviews || []);
  const [rating, setRating] = useState(5);
  const [comment, setComment] = useState("");

  const averageScore = reviews.length > 0
    ? (reviews.reduce((total, review) => total + review.rating, 0) / reviews.length).toFixed(1)
    : "0.0";

  const addReview = () => {
    const trimmed = comment.trim();
    if (!trimmed) {
      return;
    }

    const newReview = {
      id: Date.now().toString(),
      name: "You",
      rating,
      comment: trimmed,
      date: new Date().toLocaleDateString("en-US", {
        month: "short",
        day: "numeric",
        year: "numeric",
      }),
    };

    // Add the new review to the local state
    setReviews(prevReviews => [newReview, ...prevReviews]);

    // Reset the form
    setRating(5);
    setComment("");
  };

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      {mentor && (
        <View style={styles.mentorHeader}>
          <Text style={styles.mentorName}>{mentor.name}</Text>
          <Text style={styles.mentorSubject}>{mentor.subject} Tutor</Text>
          <View style={styles.mentorRating}>
            <Text style={styles.mentorRatingIcon}>⭐</Text>
            <Text style={styles.mentorRatingText}>{averageScore}</Text>
          </View>
        </View>
      )}

      <View style={styles.heroCard}>
        <Text style={styles.title}>
          {mentor ? `${mentor.name}'s Reviews` : "Student Reviews"}
        </Text>
        <Text style={[styles.subtitle, { marginBottom: reviews.length > 0 ? 18 : 0 }]}>
          {mentor
            ? "Read feedback from students who worked with this mentor."
            : "Real feedback from learners who found the right mentor."
          }
        </Text>
        {reviews.length > 0 && (
          <View style={styles.heroStats}>
            <View style={styles.heroStatCard}>
              <Text style={styles.heroStatValue}>{reviews.length}</Text>
              <Text style={styles.heroStatLabel}>Reviews</Text>
            </View>
            <View style={styles.heroStatCard}>
              <Text style={styles.heroStatValue}>{averageScore}</Text>
              <Text style={styles.heroStatLabel}>Average rating</Text>
            </View>
          </View>
        )}
      </View>

      <View style={styles.formCard}>
        <Text style={styles.formLabel}>Your Rating</Text>
        <View style={styles.ratingRow}>
          {[1, 2, 3, 4, 5].map((value) => (
            <TouchableOpacity
              key={value}
              onPress={() => setRating(value)}
              style={styles.starButton}
            >
              <Text style={[styles.star, rating >= value && styles.starActive]}>
                {rating >= value ? "★" : "☆"}
              </Text>
            </TouchableOpacity>
          ))}
        </View>

        <Text style={styles.formLabel}>Write a review</Text>
        <TextInput
          value={comment}
          onChangeText={setComment}
          placeholder="Share your experience..."
          placeholderTextColor="#94A3B8"
          style={styles.textArea}
          multiline
        />

        <TouchableOpacity style={styles.submitButton} onPress={addReview}>
          <Text style={styles.submitButtonText}>Submit Review</Text>
        </TouchableOpacity>
      </View>

      {reviews.length > 0 ? (
        <>
          <View style={styles.sectionHeader}>
            <Text style={styles.sectionTitle}>Latest Reviews</Text>
            <Text style={styles.sectionSubtitle}>{reviews.length} reviews</Text>
          </View>

          {reviews.map((review) => (
            <View key={review.id} style={styles.reviewCard}>
              <View style={styles.reviewHeader}>
                <Text style={styles.reviewName}>{review.name}</Text>
                <Text style={styles.reviewDate}>{review.date}</Text>
              </View>
              <View style={styles.ratingRowSmall}>
                {Array.from({ length: 5 }, (_, index) => (
                  <Text key={index} style={styles.smallStar}>
                    {index < review.rating ? "★" : "☆"}
                  </Text>
                ))}
              </View>
              <Text style={styles.reviewComment}>{review.comment}</Text>
            </View>
          ))}
        </>
      ) : (
        <View style={styles.emptyState}>
          <Text style={styles.emptyIcon}>📝</Text>
          <Text style={styles.emptyText}>No reviews yet</Text>
          <Text style={styles.emptySubtext}>
            {mentor ? `Be the first to review ${mentor.name}!` : "Reviews will appear here once students share their experiences."}
          </Text>
        </View>
      )}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: "#EEF2FF",
  },
  content: {
    padding: 16,
    paddingBottom: 32,
  },
  mentorHeader: {
    backgroundColor: "#FFFFFF",
    borderRadius: 20,
    padding: 20,
    marginBottom: 16,
    alignItems: "center",
    shadowColor: "#000",
    shadowOpacity: 0.05,
    shadowRadius: 10,
    elevation: 3,
  },
  mentorName: {
    fontSize: 24,
    fontWeight: "bold",
    color: "#1E3A8A",
    marginBottom: 4,
  },
  mentorSubject: {
    fontSize: 16,
    color: "#2563EB",
    fontWeight: "600",
    marginBottom: 12,
  },
  mentorRating: {
    flexDirection: "row",
    alignItems: "center",
    backgroundColor: "#FCE7A9",
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 16,
  },
  mentorRatingIcon: {
    fontSize: 16,
    marginRight: 6,
  },
  mentorRatingText: {
    fontSize: 16,
    fontWeight: "bold",
    color: "#92400E",
  },
  heroCard: {
    backgroundColor: "#2563EB",
    borderRadius: 24,
    padding: 22,
    marginBottom: 20,
    shadowColor: "#000",
    shadowOpacity: 0.12,
    shadowRadius: 18,
    shadowOffset: { width: 0, height: 10 },
    elevation: 7,
  },
  title: {
    fontSize: 28,
    fontWeight: "bold",
    color: "#FFFFFF",
    marginBottom: 10,
  },
  subtitle: {
    color: "#DBEAFE",
    fontSize: 15,
    lineHeight: 22,
    marginBottom: 18,
  },
  heroStats: {
    flexDirection: "row",
    justifyContent: "space-between",
  },
  heroStatCard: {
    flex: 1,
    backgroundColor: "rgba(255,255,255,0.15)",
    borderRadius: 18,
    padding: 16,
    marginRight: 12,
  },
  heroStatValue: {
    color: "#fff",
    fontSize: 24,
    fontWeight: "bold",
    marginBottom: 6,
  },
  heroStatLabel: {
    color: "#E0E7FF",
    fontSize: 12,
    textTransform: "uppercase",
    letterSpacing: 0.8,
  },
  formCard: {
    backgroundColor: "#ffffff",
    borderRadius: 22,
    padding: 20,
    marginBottom: 22,
    shadowColor: "#000",
    shadowOpacity: 0.06,
    shadowRadius: 14,
    shadowOffset: { width: 0, height: 6 },
    elevation: 4,
  },
  formLabel: {
    color: "#334155",
    fontWeight: "700",
    marginBottom: 10,
  },
  ratingRow: {
    flexDirection: "row",
    marginBottom: 18,
  },
  starButton: {
    marginRight: 8,
  },
  star: {
    fontSize: 30,
    color: "#CBD5E1",
  },
  starActive: {
    color: "#FACC15",
  },
  textArea: {
    minHeight: 100,
    backgroundColor: "#F8FAFC",
    borderRadius: 18,
    borderWidth: 1,
    borderColor: "#E2E8F0",
    padding: 16,
    textAlignVertical: "top",
    color: "#0F172A",
    marginBottom: 16,
  },
  submitButton: {
    backgroundColor: "#2563EB",
    paddingVertical: 16,
    borderRadius: 18,
    alignItems: "center",
  },
  submitButtonText: {
    color: "#ffffff",
    fontWeight: "bold",
    fontSize: 16,
  },
  sectionHeader: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    marginBottom: 14,
  },
  sectionTitle: {
    fontSize: 18,
    fontWeight: "bold",
    color: "#0F172A",
  },
  sectionSubtitle: {
    color: "#94A3B8",
    fontWeight: "600",
  },
  reviewCard: {
    backgroundColor: "#ffffff",
    borderRadius: 22,
    padding: 18,
    marginBottom: 14,
    shadowColor: "#000",
    shadowOpacity: 0.05,
    shadowRadius: 10,
    shadowOffset: { width: 0, height: 6 },
    elevation: 3,
  },
  reviewHeader: {
    flexDirection: "row",
    justifyContent: "space-between",
    marginBottom: 10,
  },
  reviewName: {
    fontSize: 16,
    fontWeight: "bold",
    color: "#1E3A8A",
  },
  reviewDate: {
    color: "#94A3B8",
    fontSize: 12,
  },
  ratingRowSmall: {
    flexDirection: "row",
    marginBottom: 10,
  },
  smallStar: {
    color: "#FACC15",
    fontSize: 16,
    marginRight: 4,
  },
  reviewComment: {
    color: "#475569",
    lineHeight: 22,
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
    lineHeight: 20,
  },
});
