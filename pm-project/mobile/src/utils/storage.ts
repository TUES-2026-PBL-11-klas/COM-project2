import AsyncStorage from "@react-native-async-storage/async-storage";

const TOKEN_KEY = "auth_token";

export async function saveToken(token: string) {
  await AsyncStorage.setItem(TOKEN_KEY, token);
}

// Safely get token with timeout and error handling to avoid hangs
export async function getToken(timeoutMs = 5000) {
  try {
    const getItemPromise = AsyncStorage.getItem(TOKEN_KEY);

    const timeoutPromise = new Promise<null>((resolve) =>
      setTimeout(() => resolve(null), timeoutMs)
    );

    const result = await Promise.race([getItemPromise, timeoutPromise]);
    return result as string | null;
  } catch (_err) {
    return null;
  }
}

export async function removeToken() {
  await AsyncStorage.removeItem(TOKEN_KEY);
}
