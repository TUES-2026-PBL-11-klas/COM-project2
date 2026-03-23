import { useState } from "react";
import { API_URL } from "../constants/api";
import { loginUser, registerUser } from "../services/authService";
import { saveToken } from "../utils/storage";

export const useAuth = () => {
  const [loading, setLoading] = useState(false);

  const loginMe = async (username: string, password: string) => {
    setLoading(true);
    try {
      const data = await loginUser(username, password);
      await saveToken(data.token);
      return data;
    } finally {
      setLoading(false);
    }
  };

  const registerMe = async (
    username: string,
    email: string,
    password: string
  ) => {
    setLoading(true);
    try {
      const data = await registerUser(username, email, password);
      await saveToken(data.token);
      return data;
    } finally {
      setLoading(false);
    }
  };

  return { loginMe, registerMe, loading };
};