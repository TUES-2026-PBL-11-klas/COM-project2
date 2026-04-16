import AsyncStorage from "@react-native-async-storage/async-storage";

const TOKEN_KEY = "auth_token";
const USER_ID_KEY = "user_id";

export async function saveToken(token: string) {
  await AsyncStorage.setItem(TOKEN_KEY, token);
}

export async function getToken() {
  return AsyncStorage.getItem(TOKEN_KEY);
}

export async function removeToken() {
  await AsyncStorage.removeItem(TOKEN_KEY);
}

export async function saveUserId(id: string) {
  await AsyncStorage.setItem(USER_ID_KEY, id);
}

export async function getUserId() {
  return AsyncStorage.getItem(USER_ID_KEY);
}

export async function removeUserId() {
  await AsyncStorage.removeItem(USER_ID_KEY);
}
