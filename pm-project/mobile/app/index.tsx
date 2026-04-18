import { useEffect } from "react";
import { useRouter } from "expo-router";
import { getToken } from "../src/utils/storage";
import { ActivityIndicator, View } from "react-native";

export default function Index() {
  const router = useRouter();

  useEffect(() => {
    const checkAuth = async () => {
      try {
        const token = await getToken();

        if (token) {
          router.replace("/tabs");
        } else {
          router.replace("/auth/login");
        }
      } catch (error) {
        console.error("Auth check failed:", error);
        router.replace("/auth/login");
      }
    };

    checkAuth();
  }, [router]);

  return (
    <View style={{ flex: 1, justifyContent: "center", alignItems: "center" }}>
      <ActivityIndicator size="large" color="#0000ff" />
    </View>
  );
}