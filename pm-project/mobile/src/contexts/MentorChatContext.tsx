import React, { createContext, useContext, useState, ReactNode } from 'react';

interface Mentor {
  id: string;
  name: string;
  subject?: string;
  subjects?: string | string[];
  subjectsArray?: string[];
  rating: number;
  experience: string;
  students: number;
  available: boolean;
  reviews: Review[];
}

interface Review {
  id: string;
  name: string;
  rating: number;
  comment: string;
  date: string;
}

interface MentorChatContextType {
  selectedMentorForChat: Mentor | null;
  setSelectedMentorForChat: (mentor: Mentor | null) => void;
}

const MentorChatContext = createContext<MentorChatContextType | undefined>(undefined);

export const MentorChatProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [selectedMentorForChat, setSelectedMentorForChat] = useState<Mentor | null>(null);

  return (
    <MentorChatContext.Provider value={{ selectedMentorForChat, setSelectedMentorForChat }}>
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