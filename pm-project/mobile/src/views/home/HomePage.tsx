import { useRouter } from "expo-router";
import { View, Text, TouchableOpacity, StyleSheet, ScrollView } from "react-native";
import { useEffect, useState } from "react";
import { getToken } from "../../utils/storage";

export default function HomePage() {
  const router = useRouter();
  const [isLoggedIn, setIsLoggedIn] = useState(false);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    // Check if user is logged in
    const checkAuth = async () => {
      const token = await getToken();
      setIsLoggedIn(!!token);
      setLoading(false);
    };

    checkAuth();
  }, []);

  if (loading) {
    return (
      <View style={styles.container}>
        <Text>Loading...</Text>
      </View>
    );
  }

  if (isLoggedIn) {
    // If user is logged in, go to mentors page
    return (
      <View style={styles.container}>
        <Text>Redirecting...</Text>
      </View>
    );
  }

  return (
    <ScrollView style={styles.container} showsVerticalScrollIndicator={false}>
      {/* Hero Section */}
      <View style={styles.heroSection}>
        <View style={styles.heroContent}>
          <Text style={styles.heroTitle}>Master Any Subject</Text>
          <Text style={styles.heroSubtitle}>Connect with expert mentors and unlock your potential</Text>
        </View>
      </View>

      <View style={styles.content}>
        {/* Stats Section */}
        <View style={styles.statsSection}>
          <View style={styles.statCard}>
            <Text style={styles.statNumber}>500+</Text>
            <Text style={styles.statLabel}>Expert Mentors</Text>
          </View>
          <View style={styles.statCard}>
            <Text style={styles.statNumber}>10K+</Text>
            <Text style={styles.statLabel}>Active Students</Text>
          </View>
          <View style={styles.statCard}>
            <Text style={styles.statNumber}>4.8★</Text>
            <Text style={styles.statLabel}>Average Rating</Text>
          </View>
        </View>

        {/* Features Section */}
        <View style={styles.featuresSection}>
          <Text style={styles.sectionTitle}>Why Choose Us?</Text>
          <View style={styles.featureCard}>
            <Text style={styles.featureIcon}>🎓</Text>
            <View style={styles.featureContent}>
              <Text style={styles.featureTitle}>Expert Mentors</Text>
              <Text style={styles.featureDescription}>Vetted professionals with years of teaching experience</Text>
            </View>
          </View>

          <View style={styles.featureCard}>
            <Text style={styles.featureIcon}>⭐</Text>
            <View style={styles.featureContent}>
              <Text style={styles.featureTitle}>Quality Assured</Text>
              <Text style={styles.featureDescription}>All mentors rated and reviewed by verified students</Text>
            </View>
          </View>

          <View style={styles.featureCard}>
            <Text style={styles.featureIcon}>🚀</Text>
            <View style={styles.featureContent}>
              <Text style={styles.featureTitle}>Fast Results</Text>
              <Text style={styles.featureDescription}>See measurable improvement in weeks</Text>
            </View>
          </View>

          <View style={styles.featureCard}>
            <Text style={styles.featureIcon}>💬</Text>
            <View style={styles.featureContent}>
              <Text style={styles.featureTitle}>One-on-One Support</Text>
              <Text style={styles.featureDescription}>Personalized learning at your own pace</Text>
            </View>
          </View>

          <View style={styles.featureCard}>
            <Text style={styles.featureIcon}>🎯</Text>
            <View style={styles.featureContent}>
              <Text style={styles.featureTitle}>Flexible Scheduling</Text>
              <Text style={styles.featureDescription}>Learn when and where it suits you best</Text>
            </View>
          </View>

          <View style={styles.featureCard}>
            <Text style={styles.featureIcon}>💰</Text>
            <View style={styles.featureContent}>
              <Text style={styles.featureTitle}>Affordable Pricing</Text>
              <Text style={styles.featureDescription}>Quality education at reasonable rates</Text>
            </View>
          </View>
        </View>

        {/* Subjects Section */}
        <View style={styles.subjectsSection}>
          <Text style={styles.sectionTitle}>Popular Subjects</Text>
          <View style={styles.subjectGrid}>
            <View style={styles.subjectTag}>
              <Text style={styles.subjectText}>Math</Text>
            </View>
            <View style={styles.subjectTag}>
              <Text style={styles.subjectText}>English</Text>
            </View>
            <View style={styles.subjectTag}>
              <Text style={styles.subjectText}>Physics</Text>
            </View>
            <View style={styles.subjectTag}>
              <Text style={styles.subjectText}>Chemistry</Text>
            </View>
            <View style={styles.subjectTag}>
              <Text style={styles.subjectText}>History</Text>
            </View>
            <View style={styles.subjectTag}>
              <Text style={styles.subjectText}>Programming</Text>
            </View>
          </View>
        </View>

        {/* Testimonials */}
        <View style={styles.testimonialsSection}>
          <Text style={styles.sectionTitle}>Student Reviews</Text>
          <View style={styles.testimonialCard}>
            <View style={styles.testimonialHeader}>
              <Text style={styles.testimonialName}>Sarah M.</Text>
              <Text style={styles.testimonialRating}>⭐⭐⭐⭐⭐</Text>
            </View>
            <Text style={styles.testimonialText}>&quot;Best tutoring experience ever! My grades improved from C to A in just 2 months.&quot;</Text>
          </View>
          <View style={styles.testimonialCard}>
            <View style={styles.testimonialHeader}>
              <Text style={styles.testimonialName}>John D.</Text>
              <Text style={styles.testimonialRating}>⭐⭐⭐⭐⭐</Text>
            </View>
            <Text style={styles.testimonialText}>&quot;The mentor matched me perfectly and explained concepts so clearly. Highly recommend!&quot;</Text>
          </View>
        </View>

        {/* CTA Section */}
        <View style={styles.ctaSection}>
          <Text style={styles.ctaTitle}>Ready to Transform Your Learning?</Text>
          <Text style={styles.ctaDescription}>Join thousands of successful students today</Text>

          <TouchableOpacity
            style={styles.primaryButton}
            onPress={() => router.push("/auth/login")}
          >
            <Text style={styles.primaryButtonText}>Login to Browse Mentors</Text>
          </TouchableOpacity>

          <TouchableOpacity
            style={styles.secondaryButton}
            onPress={() => router.push("/auth/register")}
          >
            <Text style={styles.secondaryButtonText}>Create New Account</Text>
          </TouchableOpacity>
        </View>

        {/* Demo Info */}
        <View style={styles.demoInfo}>
          <Text style={styles.demoTitle}>🎁 Try Demo Account</Text>
          <Text style={styles.demoText}>Username: <Text style={styles.demoValue}>demo</Text></Text>
          <Text style={styles.demoText}>Password: <Text style={styles.demoValue}>demo123</Text></Text>
          <Text style={styles.demoNote}>Perfect way to explore the platform before committing</Text>
        </View>
      </View>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: "#F8FAFC",
  },
  heroSection: {
    backgroundColor: "#2563EB",
    paddingVertical: 60,
    paddingHorizontal: 20,
    alignItems: "center",
    justifyContent: "center",
  },
  heroContent: {
    alignItems: "center",
  },
  heroTitle: {
    fontSize: 36,
    fontWeight: "bold",
    color: "#fff",
    marginBottom: 12,
    textAlign: "center",
  },
  heroSubtitle: {
    fontSize: 16,
    color: "#fff",
    textAlign: "center",
    opacity: 0.9,
  },
  content: {
    paddingHorizontal: 20,
    paddingTop: 30,
    paddingBottom: 40,
  },
  statsSection: {
    flexDirection: "row",
    justifyContent: "space-between",
    marginBottom: 40,
    marginTop: -30,
  },
  statCard: {
    flex: 1,
    backgroundColor: "#fff",
    padding: 16,
    borderRadius: 12,
    alignItems: "center",
    marginHorizontal: 4,
    shadowColor: "#000",
    shadowOpacity: 0.08,
    shadowRadius: 8,
    elevation: 4,
  },
  statNumber: {
    fontSize: 24,
    fontWeight: "bold",
    color: "#2563EB",
    marginBottom: 4,
  },
  statLabel: {
    fontSize: 12,
    color: "#64748B",
    textAlign: "center",
    fontWeight: "600",
  },
  sectionTitle: {
    fontSize: 24,
    fontWeight: "bold",
    color: "#1E3A8A",
    marginBottom: 20,
    marginTop: 10,
  },
  featuresSection: {
    marginBottom: 40,
  },
  featureCard: {
    backgroundColor: "#fff",
    padding: 16,
    paddingVertical: 14,
    borderRadius: 12,
    marginBottom: 12,
    flexDirection: "row",
    alignItems: "flex-start",
    shadowColor: "#000",
    shadowOpacity: 0.05,
    shadowRadius: 6,
    elevation: 2,
  },
  featureIcon: {
    fontSize: 28,
    marginRight: 14,
    marginTop: 2,
  },
  featureContent: {
    flex: 1,
  },
  featureTitle: {
    fontSize: 16,
    fontWeight: "bold",
    color: "#1E3A8A",
    marginBottom: 4,
  },
  featureDescription: {
    fontSize: 13,
    color: "#64748B",
    lineHeight: 18,
  },
  subjectsSection: {
    marginBottom: 40,
  },
  subjectGrid: {
    flexDirection: "row",
    flexWrap: "wrap",
    justifyContent: "space-between",
  },
  subjectTag: {
    backgroundColor: "#EFF6FF",
    paddingHorizontal: 16,
    paddingVertical: 10,
    borderRadius: 20,
    borderWidth: 1,
    borderColor: "#BFDBFE",
    marginBottom: 10,
    width: "48%",
    alignItems: "center",
  },
  subjectText: {
    color: "#1E40AF",
    fontWeight: "600",
    fontSize: 13,
  },
  testimonialsSection: {
    marginBottom: 40,
  },
  testimonialCard: {
    backgroundColor: "#fff",
    padding: 16,
    borderRadius: 12,
    marginBottom: 12,
    borderLeftWidth: 4,
    borderLeftColor: "#2563EB",
    shadowColor: "#000",
    shadowOpacity: 0.05,
    shadowRadius: 6,
    elevation: 2,
  },
  testimonialHeader: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    marginBottom: 8,
  },
  testimonialName: {
    fontSize: 14,
    fontWeight: "bold",
    color: "#1E3A8A",
  },
  testimonialRating: {
    fontSize: 12,
  },
  testimonialText: {
    fontSize: 13,
    color: "#64748B",
    lineHeight: 18,
    fontStyle: "italic",
  },
  ctaSection: {
    backgroundColor: "#fff",
    padding: 24,
    borderRadius: 16,
    marginBottom: 20,
    shadowColor: "#000",
    shadowOpacity: 0.08,
    shadowRadius: 10,
    elevation: 5,
  },
  ctaTitle: {
    fontSize: 22,
    fontWeight: "bold",
    color: "#1E3A8A",
    marginBottom: 8,
    textAlign: "center",
  },
  ctaDescription: {
    fontSize: 15,
    color: "#64748B",
    textAlign: "center",
    marginBottom: 20,
  },
  primaryButton: {
    backgroundColor: "#2563EB",
    paddingVertical: 14,
    paddingHorizontal: 24,
    borderRadius: 10,
    marginBottom: 12,
    alignItems: "center",
  },
  primaryButtonText: {
    color: "#fff",
    fontSize: 16,
    fontWeight: "bold",
  },
  secondaryButton: {
    backgroundColor: "#f0f4f8",
    paddingVertical: 14,
    paddingHorizontal: 24,
    borderRadius: 10,
    borderWidth: 2,
    borderColor: "#2563EB",
    alignItems: "center",
  },
  secondaryButtonText: {
    color: "#2563EB",
    fontSize: 16,
    fontWeight: "bold",
  },
  demoInfo: {
    backgroundColor: "#FEF08A",
    padding: 16,
    borderRadius: 12,
    borderLeftWidth: 4,
    borderLeftColor: "#EAB308",
  },
  demoTitle: {
    fontSize: 15,
    fontWeight: "700",
    color: "#854D0E",
    marginBottom: 10,
  },
  demoText: {
    fontSize: 13,
    color: "#854D0E",
    marginBottom: 4,
  },
  demoValue: {
    fontWeight: "bold",
    backgroundColor: "#FEFCE8",
    paddingHorizontal: 4,
  },
  demoNote: {
    fontSize: 12,
    color: "#854D0E",
    marginTop: 8,
    fontStyle: "italic",
  },
});
