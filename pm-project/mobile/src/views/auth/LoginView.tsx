import { useState } from "react";
import {
  View,
  Text,
  TextInput,
  TouchableOpacity,
  StyleSheet,
  Alert,
} from "react-native";
import { loginVM } from "../../viewmodels/auth/authViewModel";
import { useRouter } from "expo-router";

export default function Login() {
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [loading, setLoading] = useState(false);

  const router = useRouter();

  const handleLogin = async () => {
    if (!username || !password) {
      Alert.alert("Error", "Please enter both username and password");
      return;
    }

    setLoading(true);
    try {
      const res = await loginVM(username, password);

      if (res.token) {
        setUsername("");
        setPassword("");
        router.replace("/tabs");
      } else {
        Alert.alert("Error", "No token received");
      }
    } catch (err: any) {
      Alert.alert("Login failed", err.message || "An error occurred");
    } finally {
      setLoading(false);
    }
  };

  return (
    <View style={styles.container}>
      <View style={styles.card}>
        <Text style={styles.title}>Welcome Back</Text>
        <Text style={styles.subtitle}>Login to your account</Text>

        <TextInput
          placeholder="Username"
          value={username}
          onChangeText={setUsername}
          style={styles.input}
          editable={!loading}
        />

        <TextInput
          placeholder="Password"
          secureTextEntry
          value={password}
          onChangeText={setPassword}
          style={styles.input}
          editable={!loading}
        />

        <TouchableOpacity 
          style={[styles.button, loading && styles.buttonDisabled]} 
          onPress={handleLogin}
          disabled={loading}
        >
          <Text style={styles.buttonText}>
            {loading ? "Logging in..." : "Login"}
          </Text>
        </TouchableOpacity>

        <Text style={styles.link} onPress={() => router.push("/auth/register")}>
          Don’t have an account? Register
        </Text>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: "#F5F8FF",
    justifyContent: "center",
    alignItems: "center",
  },
  card: {
    width: "85%",
    backgroundColor: "#fff",
    padding: 20,
    borderRadius: 16,
    shadowColor: "#000",
    shadowOpacity: 0.1,
    shadowRadius: 10,
    elevation: 5,
  },
  title: {
    fontSize: 26,
    fontWeight: "bold",
    color: "#1E3A8A",
    marginBottom: 5,
  },
  subtitle: {
    color: "#6B7280",
    marginBottom: 20,
  },
  input: {
    borderWidth: 1,
    borderColor: "#D1D5DB",
    borderRadius: 10,
    padding: 12,
    marginBottom: 12,
    backgroundColor: "#F9FAFB",
  },
  button: {
    backgroundColor: "#2563EB",
    padding: 14,
    borderRadius: 10,
    alignItems: "center",
    marginTop: 10,
  },
  buttonDisabled: {
    backgroundColor: "#93C5FD",
    opacity: 0.7,
  },
  buttonText: {
    color: "#fff",
    fontWeight: "bold",
    fontSize: 16,
  },
  link: {
    marginTop: 15,
    textAlign: "center",
    color: "#2563EB",
  },
});
