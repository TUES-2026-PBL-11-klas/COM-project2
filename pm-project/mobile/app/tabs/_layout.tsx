import { Tabs } from "expo-router";
import { Ionicons } from "@expo/vector-icons";
import { MentorReviewsProvider } from "../../src/contexts/MentorReviewsContext";
import { MentorChatProvider } from "../../src/contexts/MentorChatContext";

const iconMap: Record<string, string> = {
  index: "home",
  chat: "chatbubble-ellipses",
  reviews: "star",
};

export default function TabsLayout() {
  return (
    <MentorReviewsProvider>
      <MentorChatProvider>
        <Tabs
          screenOptions={({ route }) => ({
            headerShown: false,
            tabBarActiveTintColor: "#2563EB",
            tabBarInactiveTintColor: "#64748B",
            tabBarLabelStyle: {
              fontSize: 12,
              fontWeight: "700",
              marginBottom: 4,
            },
            tabBarStyle: {
              backgroundColor: "#ffffff",
              borderTopWidth: 0,
              height: 70,
              paddingTop: 8,
              paddingBottom: 10,
              shadowColor: "#000",
              shadowOpacity: 0.08,
              shadowRadius: 18,
              shadowOffset: { width: 0, height: -4 },
              elevation: 12,
            },
            tabBarIcon: ({ color, size }) => {
              const iconName = (iconMap[route.name] || "home") as keyof typeof Ionicons.glyphMap;
              return <Ionicons name={iconName} size={size} color={color} />;
            },
          })}
        />
      </MentorChatProvider>
    </MentorReviewsProvider>
  );
}
