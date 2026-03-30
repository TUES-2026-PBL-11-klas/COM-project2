import { useRouter } from "expo-router";
import { useEffect } from "react";
import HomePage from "../view/home/HomePage";

export default function Home() {
  const router = useRouter();

  // Check if user is logged in and redirect to tabs
  useEffect(() => {
    const checkAndRedirect = async () => {
      const { getToken } = await import("../utils/storage");
      const token = await getToken();
      if (token) {
        router.replace("/tabs");
      }
    };

    checkAndRedirect();
  }, []);

  return <HomePage />;
}
