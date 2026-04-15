import { useState } from "react";
import {
  View,
  Text,
  FlatList,
  TextInput,
  TouchableOpacity,
  StyleSheet,
  KeyboardAvoidingView,
  Platform,
} from "react-native";
import { useMentorChat } from "../../contexts/MentorChatContext";

export default function ChatView() {
  const { selectedMentorForChat } = useMentorChat();

  const initialMessages = selectedMentorForChat ? [
    {
      id: "1",
      sender: selectedMentorForChat.name,
      text: `Hi! I'm ${selectedMentorForChat.name}, your ${selectedMentorForChat.subject} tutor. How can I help you today?`,
      time: "09:12",
    },
  ] : [];

  const [messages, setMessages] = useState(initialMessages);
  const [draft, setDraft] = useState("");

  const sendMessage = () => {
    const trimmed = draft.trim();
    if (!trimmed) {
      return;
    }

    const newMessage = {
      id: Date.now().toString(),
      sender: "You",
      text: trimmed,
      time: new Date().toLocaleTimeString([], {
        hour: "2-digit",
        minute: "2-digit",
      }),
    };

    setMessages((prev) => [...prev, newMessage]);
    setDraft("");

    setTimeout(() => {
      setMessages((prev) => [
        ...prev,
        {
          id: Date.now().toString() + "-reply",
          sender: selectedMentorForChat?.name || "Mentor",
          text: "That sounds good. Let me explain that concept further.",
          time: new Date().toLocaleTimeString([], {
            hour: "2-digit",
            minute: "2-digit",
          }),
        },
      ]);
    }, 900);
  };

  return (
    <KeyboardAvoidingView
      behavior={Platform.OS === "ios" ? "padding" : "height"}
      style={styles.container}
    >
      <View style={styles.heroCard}>
        <Text style={styles.title}>
          {selectedMentorForChat ? `Chat with ${selectedMentorForChat.name}` : "Mentor Chat"}
        </Text>
        <Text style={styles.subtitle}>
          {selectedMentorForChat
            ? `Get help with ${selectedMentorForChat.subject} from your personal tutor.`
            : "Quick support from your tutor. Ask any question and keep learning."
          }
        </Text>
        <View style={styles.statusBadge}>
          <Text style={styles.statusText}>
            {selectedMentorForChat ? `${selectedMentorForChat.name} online` : "Mentor online"}
          </Text>
        </View>
      </View>

      <FlatList
        data={messages}
        keyExtractor={(item) => item.id}
        contentContainerStyle={styles.messageList}
        renderItem={({ item }) => {
          const isUser = item.sender === "You";
          return (
            <View
              style={[
                styles.messageBubble,
                isUser ? styles.userBubble : styles.mentorBubble,
              ]}
            >
              <Text style={[styles.messageText, isUser && styles.userText]}>
                {item.text}
              </Text>
              <Text style={[styles.messageTime, isUser && styles.userTime]}>
                {item.time}
              </Text>
            </View>
          );
        }}
      />

      <View style={styles.inputRow}>
        <TextInput
          value={draft}
          onChangeText={setDraft}
          placeholder="Type a message..."
          placeholderTextColor="#94A3B8"
          style={styles.input}
          multiline
        />
        <TouchableOpacity style={styles.sendButton} onPress={sendMessage}>
          <Text style={styles.sendButtonText}>Send</Text>
        </TouchableOpacity>
      </View>
    </KeyboardAvoidingView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: "#EEF2FF",
    paddingHorizontal: 16,
    paddingTop: 18,
  },
  heroCard: {
    backgroundColor: "#2563EB",
    borderRadius: 24,
    padding: 20,
    marginBottom: 16,
    shadowColor: "#000",
    shadowOpacity: 0.12,
    shadowRadius: 18,
    shadowOffset: { width: 0, height: 10 },
    elevation: 7,
  },
  title: {
    fontSize: 28,
    fontWeight: "bold",
    color: "#fff",
    marginBottom: 8,
  },
  subtitle: {
    fontSize: 15,
    lineHeight: 22,
    color: "#DBEAFE",
    marginBottom: 16,
  },
  statusBadge: {
    alignSelf: "flex-start",
    backgroundColor: "rgba(255,255,255,0.16)",
    paddingVertical: 8,
    paddingHorizontal: 14,
    borderRadius: 18,
  },
  statusText: {
    color: "#fff",
    fontWeight: "700",
  },
  messageList: {
    paddingBottom: 14,
  },
  messageBubble: {
    maxWidth: "80%",
    padding: 16,
    borderRadius: 22,
    marginBottom: 10,
    shadowColor: "#000",
    shadowOpacity: 0.05,
    shadowRadius: 10,
    shadowOffset: { width: 0, height: 6 },
    elevation: 3,
  },
  mentorBubble: {
    alignSelf: "flex-start",
    backgroundColor: "#FFFFFF",
  },
  userBubble: {
    alignSelf: "flex-end",
    backgroundColor: "#2563EB",
  },
  messageText: {
    color: "#0F172A",
    fontSize: 15,
    lineHeight: 22,
  },
  userText: {
    color: "#fff",
  },
  messageTime: {
    marginTop: 10,
    fontSize: 11,
    color: "#94A3B8",
    textAlign: "right",
  },
  userTime: {
    color: "#DCE7FF",
  },
  inputRow: {
    flexDirection: "row",
    alignItems: "flex-end",
    marginBottom: 18,
  },
  input: {
    flex: 1,
    minHeight: 52,
    maxHeight: 110,
    backgroundColor: "#fff",
    borderRadius: 18,
    paddingHorizontal: 16,
    paddingVertical: 14,
    borderWidth: 1,
    borderColor: "#E2E8F0",
    color: "#0F172A",
    marginRight: 12,
  },
  sendButton: {
    backgroundColor: "#1D4ED8",
    paddingVertical: 16,
    paddingHorizontal: 18,
    borderRadius: 18,
    justifyContent: "center",
    alignItems: "center",
    minWidth: 84,
  },
  sendButtonText: {
    color: "#ffffff",
    fontWeight: "700",
  },
});
