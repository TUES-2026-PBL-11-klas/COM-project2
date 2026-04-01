import { API_URL } from "../constants/api";
import { request } from "./apiClient";

export async function loginUser(username: string, password: string) {
  return request(`${API_URL}/Auth/login`, "POST", {
    username,
    password,
  });
}

export async function registerUser(
  username: string,
  email: string,
  password: string
) {
  return request(`${API_URL}/Auth/register`, "POST", {
    username,
    email,
    password,
  });
}

export async function getMe(token: string) {
  return request(`${API_URL}/Auth/me`, "GET", undefined, token);
}
