import { useEffect } from "react";
import { useRouter } from "expo-router";
import { getToken } from "../utils/storage";


export default function Index() {
  const router = useRouter();

  useEffect(() => {
    const checkAuth = async () => {
      const token = await getToken();

      if (token) {
        router.replace("/tabs");
      } else {
        router.replace("/home");
      }
    };

    checkAuth();
  }, []);

  return null;
}