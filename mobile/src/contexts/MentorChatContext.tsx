import React, { createContext, useContext, useState, ReactNode } from 'react';

interface Review {
  id: string;
  name: string;
  rating: number;
  comment: string;
  date: string;
}

interface Profile {
  id: string;
  name: string;
  subject: string;
  rating?: number;
  experience?: string;
  students?: number;
  available?: boolean;
  reviews?: Review[];
}

interface Message {
  id: string;
  sender: string;
  text: string;
  time: string;
}

interface MentorChatContextType {
  selectedMentorForChat: Profile | null;
  setSelectedMentorForChat: (mentor: Profile | null) => void;
  chatList: Profile[];
  setChatList: (chats: Profile[]) => void;
  messages: Record<string, Message[]>;
  addMessage: (conversationId: string, message: Message) => void;
}

const MentorChatContext = createContext<MentorChatContextType | undefined>(undefined);

export const MentorChatProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [selectedMentorForChat, setSelectedMentorForChat] = useState<Profile | null>(null);
  const [chatList, setChatList] = useState<Profile[]>([]);
  const [messages, setMessages] = useState<Record<string, Message[]>>({});

  const addMessage = (conversationId: string, message: Message) => {
    setMessages((prev) => ({
      ...prev,
      [conversationId]: [...(prev[conversationId] || []), message],
    }));
  };

  return (
    <MentorChatContext.Provider value={{ selectedMentorForChat, setSelectedMentorForChat, chatList, setChatList, messages, addMessage }}>
      {children}
    </MentorChatContext.Provider>
  );
};

export const useMentorChat = () => {
  const context = useContext(MentorChatContext);
  if (context === undefined) {
    throw new Error('useMentorChat must be used within a MentorChatProvider');
  }
  return context;
};