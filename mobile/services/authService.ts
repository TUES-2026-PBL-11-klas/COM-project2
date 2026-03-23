<<<<<<< Updated upstream
import { API_URL } from "../constants/api";
import { request } from "./apiClient";

export async function login(username: string, password: string) {
  return request(`${API_URL}/Auth/login`, "POST", {
    username,
    password,
  });
}

export async function register(
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
=======
import { API_URL } from "../constants/api";
import { request } from "./apiClient";

export async function login(username: string, password: string) {
  return request(`${API_URL}/Auth/login`, "POST", {
    username,
    password,
  });
}

export async function register(
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
>>>>>>> Stashed changes
