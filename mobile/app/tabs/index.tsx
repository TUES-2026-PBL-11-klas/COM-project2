import { View, Text, Button } from "react-native";
import { removeToken } from "../../utils/storage";
import { useRouter } from "expo-router";

export default function Home() {
  const router = useRouter();

  const logout = async () => {
    await removeToken();
    router.replace("/(auth)/login");
  };

  return (
    <View style={{ padding: 20 }}>
      <Text style={{ fontSize: 24 }}>Home</Text>

      <Button title="Logout" onPress={logout} />
    </View>
  );
}