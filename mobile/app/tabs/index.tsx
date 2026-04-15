import HomeView from "../../src/views/auth/home/HomeView";
import { useEffect } from "react";
import { useRouter } from "expo-router";

export default function Home() {
  const router = useRouter();

  useEffect(() => {
    const checkAuth = async () => {
      const { getToken } = await import("../../src/utils/storage");
      const token = await getToken();
      if (!token) {
        router.replace("/auth/login");
      }
    };

    checkAuth();
  }, [router]);

  return <HomeView />;
}