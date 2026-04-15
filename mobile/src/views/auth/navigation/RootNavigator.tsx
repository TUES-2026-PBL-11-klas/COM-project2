import { useEffect, useState } from "react";
import { useRouter } from "expo-router";
import { getToken } from "../../../utils/storage";
import { View, ActivityIndicator } from "react-native";

export default function RootNavigator() {
  const router = useRouter();
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const checkAuth = async () => {
      try {
        const token = await getToken();

        if (token) {
          router.replace("/");
        } else {
          router.replace("/auth/login");
        }
      } catch (err) {
        router.replace("/auth/login");
      } finally {
        setLoading(false);
      }
    };

    checkAuth();
  }, []);

  if (loading) {
    return (
      <View style={{ flex: 1, justifyContent: "center", alignItems: "center" }}>
        <ActivityIndicator size="large" color="#2563EB" />
      </View>
    );
  }

  return null;
}
