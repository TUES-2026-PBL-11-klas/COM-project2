import { useRouter } from "expo-router";
import { useEffect } from "react";
import HomePage from "../src/views/home/HomePage";

export default function Home() {
  const router = useRouter();

  useEffect(() => {
    const checkAndRedirect = async () => {
      const { getToken } = await import("../src/utils/storage");
      const token = await getToken();
      if (token) {
        router.replace("/tabs");
      }
    };

    checkAndRedirect();
  }, [router]);

  return <HomePage />;
}
