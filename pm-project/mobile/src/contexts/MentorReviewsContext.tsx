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

interface MentorReviewsContextType {
  selectedMentor: Mentor | null;
  setSelectedMentor: (mentor: Mentor | null) => void;
}

const MentorReviewsContext = createContext<MentorReviewsContextType | undefined>(undefined);

export const MentorReviewsProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [selectedMentor, setSelectedMentor] = useState<Mentor | null>(null);

  return (
    <MentorReviewsContext.Provider value={{ selectedMentor, setSelectedMentor }}>
      {children}
    </MentorReviewsContext.Provider>
  );
};

export const useMentorReviews = () => {
  const context = useContext(MentorReviewsContext);
  if (context === undefined) {
    throw new Error('useMentorReviews must be used within a MentorReviewsProvider');
  }
  return context;
};