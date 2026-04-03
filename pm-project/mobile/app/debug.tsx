import React, { useState } from "react";
import { View, Text, Button, ScrollView, StyleSheet } from "react-native";
import { API_URL } from "../src/constants/api";
import { loginUser, getMe } from "../src/services/authService";
import { getToken } from "../src/utils/storage";

export default function DebugScreen() {
  const [log, setLog] = useState<string[]>([]);

  const append = (line: string) => setLog((l) => [line, ...l].slice(0, 100));

  const testLogin = async () => {
    append(`Testing login -> ${API_URL}/Auth/login`);
    try {
      const res = await loginUser("demo", "demo123");
      append(`login ok: ${JSON.stringify(res)}`);
    } catch (e: any) {
      append(`login err: ${String(e?.message || e)}`);
    }
  };

  const testMe = async () => {
    const token = await getToken();
    append(`calling /Auth/me with token: ${token ? "present" : "none"}`);
    try {
      const res = await getMe(token || "");
      append(`me ok: ${JSON.stringify(res)}`);
    } catch (e: any) {
      append(`me err: ${String(e?.message || e)}`);
    }
  };

  const showApi = () => append(`API_URL=${API_URL}`);

  return (
    <View style={styles.container}>
      <Text style={styles.title}>Debug</Text>
      <View style={styles.buttons}>
        <Button title="Show API_URL" onPress={showApi} />
        <Button title="Login demo" onPress={testLogin} />
        <Button title="Call /Auth/me" onPress={testMe} />
      </View>

      <ScrollView style={styles.log}>
        {log.map((l, i) => (
          <Text key={i} style={styles.logLine}>
            {l}
          </Text>
        ))}
      </ScrollView>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, padding: 16, backgroundColor: "#fff" },
  title: { fontSize: 20, fontWeight: "bold", marginBottom: 8 },
  buttons: { flexDirection: "row", justifyContent: "space-between", gap: 8, marginBottom: 12 },
  log: { flex: 1, marginTop: 8 },
  logLine: { fontSize: 12, color: "#111", marginBottom: 6 },
});
