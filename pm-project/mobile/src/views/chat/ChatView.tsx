import { useState, useEffect } from "react";
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
import { MaterialIcons } from '@expo/vector-icons';
import { useMentorChat } from "../../contexts/MentorChatContext";
import eventBus from "../../utils/eventBus";
import { API_URL } from "../../constants/api";
import { getToken, getUserId, ensureUserId } from "../../utils/storage";
import { useRouter } from "expo-router";
import { getMentors } from "../../viewmodels/home/homeViewModel";

type MessageItem = { id: string; sender: string; text: string; time: string };

export default function ChatView() {
  const { selectedMentorForChat, setSelectedMentorForChat } = useMentorChat();
  const router = useRouter();

  const [activeChat, setActiveChat] = useState<any | null>(null);

  const initialMessages: MessageItem[] = [];

  const [messages, setMessages] = useState<MessageItem[]>(initialMessages);
  const [draft, setDraft] = useState("");
  const [mentors, setMentors] = useState<any[]>([]);

  const createAndOpen = async (m: any) => {
    try { setSelectedMentorForChat?.(null); } catch {}

    const mentorId = m.externalMentorId || m.user2Id || m.user1Id || m.id;
    const senderId = await ensureUserId();
    const token = await getToken();

    if (!senderId && !token) return;

    try {
      const body: any = { mentorId };
      if (senderId) body.senderId = senderId;

      if (String(mentorId) && senderId && String(mentorId) === String(senderId)) {
        console.warn('Cannot create a chat with yourself');
        return;
      }

      const res = await fetch(`${API_URL}/chats`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          ...(token ? { Authorization: `Bearer ${token}` } : {}),
        },
        body: JSON.stringify(body),
      });

      if (!res.ok) {
        console.warn("Failed to create chat", res.status, await res.text());
        return;
      }

      const created = await res.json();
      let me: any = null;
      if (token) {
        try {
          const rMe = await fetch(`${API_URL}/auth/me`, { headers: { Authorization: `Bearer ${token}` } });
          if (rMe.ok) me = await rMe.json();
        } catch (e) { }

        const r2 = await fetch(`${API_URL}/chats/mine`, { headers: { Authorization: `Bearer ${token}` } });
        if (r2.ok) setChats(await r2.json());
      }

      try {
        const myName = me && me.username ? String(me.username).toLowerCase() : null;
        const myId = me && me.id ? String(me.id) : null;
        const createdName = (resolveChatName(created) || '').toLowerCase();
        if ((myId && created.user1Id && String(created.user1Id) === myId && created.user2Id && String(created.user2Id) === myId)
          || (myName && createdName === myName)) {
          console.warn('Created chat resolves to current user; not opening.');
        } else {
          setActiveChat(created);
        }
      } catch (e) { setActiveChat(created); }
    } catch (err) {
      console.warn("Create/open chat error", err);
    }
  };

  useEffect(() => {
    if (!selectedMentorForChat) return;
    createAndOpen(selectedMentorForChat);
  }, [selectedMentorForChat]);

  useEffect(() => {
    (async () => {
      try {
        const m = await getMentors();
        setMentors(m || []);
      } catch (err) {
        console.warn("Load mentors error", err);
      }
    })();
  }, []);

  const resolveChatName = (chat: any) => {
    if (!chat) return "Mentor";
    if (chat.name && !chat.name.startsWith("chat_")) {
      if (/^\d+$/.test(String(chat.name))) {
        const found = mentors.find((x) => String(x.id) === String(chat.name));
        if (found) return found.name;
      }
      return chat.name;
    }

    const externalId = chat.externalMentorId || chat.user2Id || chat.user1Id || chat.id;
    const found = mentors.find((x) => x.id === String(externalId) || String(x.id) === String(externalId));
    if (found) return found.name;

    return "Mentor";
  };

  const chatExistsForMentor = (m: any) => {
    if (!m || !chats) return false;
    return chats.some((c: any) =>
      (c.externalMentorId && String(c.externalMentorId) === String(m.id)) ||
      (c.name && m.name && String(c.name).toLowerCase().includes(String(m.name).toLowerCase())) ||
      (c.user1Id && m.user1Id && String(c.user1Id) === String(m.user1Id)) ||
      (c.user2Id && m.user2Id && String(c.user2Id) === String(m.user2Id)) ||
      String(c.user1Id) === String(m.id) || String(c.user2Id) === String(m.id)
    );
  };

  useEffect(() => {
    (async () => {
      if (!activeChat) return;
      try {
        const myId = await ensureUserId();
        const token = await getToken();

        if (!myId && !token) return;

        const chatId = activeChat.id;
        const url = `${API_URL}/chats/${chatId}/messages`;

        const res = await fetch(url, {
          headers: {
            "Content-Type": "application/json",
            ...(token ? { Authorization: `Bearer ${token}` } : {}),
          },
        });

        if (!res.ok) {
          console.warn("Failed to load messages", res.status, await res.text());
          return;
        }

        const msgs = await res.json();
        const mapped = msgs.map((m: any) => ({
          id: m.id,
          sender: myId && m.senderId === myId ? "You" : resolveChatName(activeChat),
          text: m.content,
          time: new Date(m.createdAt).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" }),
        }));

        setMessages(mapped);
      } catch (err) {
        console.warn("Load messages error", err);
      }
    })();
  }, [activeChat]);

  useEffect(() => {
    const unsub1 = eventBus.on('chatsUpdated', (payload: any) => {
      try { setChats(payload || []); } catch (e) {}
    });
    const unsub2 = eventBus.on('mentorResigned', async () => {
      try {
        const token = await getToken();
        if (!token) return;
        const r = await fetch(`${API_URL}/chats/mine`, { headers: { Authorization: `Bearer ${token}` } });
        if (r.ok) setChats(await r.json());
      } catch (e) { }
    });
    return () => { if (unsub1) unsub1(); if (unsub2) unsub2(); };
  }, []);

  const [chats, setChats] = useState<any[] | null>(null);

  useEffect(() => {
    (async () => {
      if (activeChat) {
        setChats(null);
        return;
      }
      try {
        const token = await getToken();
        if (!token) return;
        let me: any = null;
        try {
          const rMe = await fetch(`${API_URL}/auth/me`, { headers: { Authorization: `Bearer ${token}` } });
          if (rMe.ok) me = await rMe.json();
        } catch (e) { /* ignore */ }

        const res = await fetch(`${API_URL}/chats/mine`, {
          headers: { Authorization: `Bearer ${token}` },
        });
        if (!res.ok) {
          console.warn("Failed to load chats", res.status, await res.text());
          return;
        }
        const data = await res.json();
        const filtered = (data || []).filter((c: any) => {
          try {
            const name = (resolveChatName(c) || '').toLowerCase();
            const myName = (me && me.username ? String(me.username) : '').toLowerCase();
            const myId = me && me.id ? String(me.id) : null;
            const EMPTY_GUID = "00000000-0000-0000-0000-000000000000";

            if (!myId && !myName) return true;

            if (myName && name === myName) return false;

            if (myId && c.id && String(c.id) === myId) return false;

            if (myId && c.externalMentorId && String(c.externalMentorId) === myId) return false;

            if (myId && c.ownerId && String(c.ownerId) === myId) return false;

            const u1 = c.user1Id ? String(c.user1Id) : null;
            const u2 = c.user2Id ? String(c.user2Id) : null;
            if (myId) {
              if (u1 === myId && (!u2 || u2 === myId || u2 === EMPTY_GUID)) return false;
              if (u2 === myId && (!u1 || u1 === myId || u1 === EMPTY_GUID)) return false;
            }

            if (myId && c.name && String(c.name).toLowerCase().includes(myId.toLowerCase())) return false;

            if (myName && c.name && String(c.name).toLowerCase().includes(myName)) return false;

          } catch (e) { }
          return true;
        });
        setChats(filtered);
      } catch (err) {
        console.warn("Load chats error", err);
      }
    })();
  }, [activeChat]);

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

    (async () => {
      try {
        const senderId = await ensureUserId();
        const token = await getToken();
        if (!activeChat) return;
        if (!senderId && !token) return;

        const body: any = { content: trimmed };
        if (senderId) body.senderId = senderId;

        const chatId = activeChat.id;

        const res = await fetch(`${API_URL}/chats/${chatId}/messages`, {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            ...(token ? { Authorization: `Bearer ${token}` } : {}),
          },
          body: JSON.stringify(body),
        });

        if (!res.ok) {
          console.warn("Failed to send message to server", await res.text());
        } else {
          const saved = await res.json();
          setMessages((prev) => prev.map(m => m.id === newMessage.id ? { ...m, id: saved.id } : m));
        }
      } catch (err) {
        console.warn("Send message error", err);
      }
    })();
  };

  return (
    <KeyboardAvoidingView
      behavior={Platform.OS === "ios" ? "padding" : "height"}
      style={styles.container}
    >
      {!activeChat && (
        <View style={{ flex: 1 }}>
          <Text style={{ fontSize: 20, fontWeight: "700", marginBottom: 12 }}>Your Chats</Text>
          {chats === null ? (
            <Text>Loading...</Text>
          ) : chats.length === 0 ? (
            <View style={styles.emptyState}>
              <MaterialIcons name="chat" size={48} color="#94A3B8" />
              <Text style={styles.emptyText}>Start chatting</Text>
              {selectedMentorForChat && chatExistsForMentor(selectedMentorForChat) ? (
                <View style={[styles.startChatButton, styles.disabledButton]}>
                  <Text style={[styles.startChatButtonText, styles.disabledButtonText]}>Already Chatting</Text>
                </View>
              ) : (
                <TouchableOpacity
                  style={styles.startChatButton}
                  disabled={selectedMentorForChat ? chatExistsForMentor(selectedMentorForChat) : false}
                  onPress={async () => {
                    if (selectedMentorForChat) {
                      await createAndOpen(selectedMentorForChat);
                    } else {
                      router.push("/");
                    }
                  }}
                >
                  <Text style={styles.startChatButtonText}>Start Chatting</Text>
                </TouchableOpacity>
              )}
            </View>
          ) : (
            <FlatList
              data={chats}
              keyExtractor={(c) => c.id}
              renderItem={({ item }) => (
                <TouchableOpacity
                  style={{ paddingVertical: 8 }}
                  onPress={() => {
                    setActiveChat(item);
                  }}
                >
                  <View style={styles.chatCardLarge}>
                    <View style={{ flexDirection: "row", alignItems: "center" }}>
                      {/* Tuka e avatara */}
                      <View style={styles.avatar}>
                        <Text style={styles.avatarText}>
                          {String(resolveChatName(item) || "").trim().split(" ")[0]?.charAt(0).toUpperCase()}
                        </Text>
                      </View>

                      {/* Tuka e imeto na chata */}
                      <View style={{ flex: 1 }}>
                        <Text style={styles.chatCardLargeTitle}>
                          {resolveChatName(item)}
                        </Text>

                        {(item.lastMessageContent || item.LastMessageContent || item.last_message_content) && (
                          <Text style={styles.chatCardSubtitle} numberOfLines={1}>
                            {item.lastMessageContent || item.LastMessageContent || item.last_message_content}
                          </Text>
                        )}
                      </View>

                    </View>
                  </View>
                </TouchableOpacity>
              )}
            />
          )}
        </View>
      )}
      {activeChat && (
        <TouchableOpacity onPress={() => setActiveChat(null)} style={{ marginBottom: 12 }}>
          <Text style={{ color: "#2563EB", fontWeight: "700" }}>← Back to chats</Text>
        </TouchableOpacity>
      )}
      {activeChat && (
        <>
          <View style={styles.heroCard}>
            <Text style={styles.title}>{`Chat with ${resolveChatName(activeChat)}`}</Text>
            <Text style={styles.subtitle}>{`Get help with ${Array.isArray(activeChat?.subjects) ? activeChat.subjects.join(', ') : (activeChat?.subject || '')} from your personal tutor.`}</Text>
            <View style={styles.statusBadge}>
              <Text style={styles.statusText}>{`${resolveChatName(activeChat)} online`}</Text>
            </View>

            {/* removed close '✕' button as requested */}
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
        </>
      )}
    </KeyboardAvoidingView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: "#EEF2FF",
    paddingHorizontal: 16,
    paddingTop: Platform.OS === "ios" ? 54 : 18,
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
  startChatButton: {
    marginTop: 16,
    backgroundColor: "#2563EB",
    paddingVertical: 12,
    paddingHorizontal: 20,
    borderRadius: 12,
  },
  startChatButtonText: {
    color: "#ffffff",
    fontWeight: "700",
  },
  emptyState: {
    alignItems: "center",
    justifyContent: "center",
    marginTop: 40,
  },
  emptyIcon: {
    fontSize: 48,
    marginBottom: 8,
  },
  emptyText: {
    fontSize: 18,
    fontWeight: "700",
    color: "#0F172A",
    marginBottom: 8,
  },
  disabledButton: {
    backgroundColor: "#E6E9EE",
  },
  disabledButtonText: {
    color: "#607080",
  },
  chatCard: {
    backgroundColor: "#2563EB",
    borderRadius: 14,
    padding: 14,
  },
  chatCardTitle: {
    color: "#fff",
    fontWeight: "700",
    marginBottom: 6,
    fontSize: 16,
  },
  chatCardSubtitle: {
    color: "rgba(255,255,255,0.85)",
    fontSize: 14,
  },
  closeButton: {
    position: "absolute",
    right: 16,
    top: Platform.OS === "ios" ? 16 : 8,
    backgroundColor: "rgba(255,255,255,0.12)",
    borderRadius: 16,
    width: 36,
    height: 36,
    alignItems: "center",
    justifyContent: "center",
  },
  closeButtonText: {
    color: "#fff",
    fontWeight: "700",
    fontSize: 18,
  },
  chatCardLarge: {
    backgroundColor: "#2563EB",
    borderRadius: 22,
    paddingVertical: 18,
    paddingHorizontal: 18,
    marginBottom: 12,
    shadowColor: "#000",
    shadowOpacity: 0.08,
    shadowRadius: 14,
    shadowOffset: { width: 0, height: 8 },
    elevation: 4,
  },
  chatCardLargeTitle: {
    color: "#fff",
    fontWeight: "700",
    marginBottom: 4,
    fontSize: 18,
  },
  avatar: {
    width: 44,
    height: 44,
    borderRadius: 22,
    backgroundColor: "#F3F4F6",
    alignItems: "center",
    justifyContent: "center",
    marginRight: 12,
  },
  avatarText: {
    color: "#1D4ED8",
    fontWeight: "700",
    fontSize: 18,
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
