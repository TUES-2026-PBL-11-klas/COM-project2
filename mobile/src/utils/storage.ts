import AsyncStorage from "@react-native-async-storage/async-storage";

const TOKEN_KEY = "auth_token";
const ROLE_KEY = "auth_role";
const USERNAME_KEY = "auth_username";

export async function saveToken(token: string) {
  await AsyncStorage.setItem(TOKEN_KEY, token);
}

export async function saveRole(role: string) {
  await AsyncStorage.setItem(ROLE_KEY, role);
}

export async function saveUsername(username: string) {
  await AsyncStorage.setItem(USERNAME_KEY, username);
}

export async function getToken() {
  return AsyncStorage.getItem(TOKEN_KEY);
}

export async function getRole() {
  return AsyncStorage.getItem(ROLE_KEY);
}

export async function getUsername() {
  return AsyncStorage.getItem(USERNAME_KEY);
}

export async function removeToken() {
  await AsyncStorage.multiRemove([TOKEN_KEY, ROLE_KEY, USERNAME_KEY]);
}
