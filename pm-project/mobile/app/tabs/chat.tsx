import ChatView from "../../src/views/chat/ChatView";
import { useEffect, useState } from "react";
import { ActivityIndicator, View } from "react-native";
import { useRouter } from "expo-router";

export default function ChatScreen() {
  const router = useRouter();
  const [authorized, setAuthorized] = useState<boolean | null>(null);

  useEffect(() => {
    const checkAuth = async () => {
      const { getToken } = await import("../../src/utils/storage");
      const token = await getToken();
      if (!token) {
        setAuthorized(false);
        router.replace("/auth/login");
        return;
      }
      setAuthorized(true);
    };

    checkAuth();
  }, [router]);

  if (authorized === null) {
    return (
      <View style={{ flex: 1, justifyContent: "center", alignItems: "center" }}>
        <ActivityIndicator size="large" color="#0000ff" />
      </View>
    );
  }

  if (!authorized) return null;

  return <ChatView />;
}
