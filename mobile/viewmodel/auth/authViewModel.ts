import { saveToken } from "../../utils/storage";
import { mockLogin, mockRegister } from "../../services/mockAuthService";

// Use mock authentication for development
// To use real backend, change these to loginUser/registerUser from authService
export async function loginVM(username: string, password: string) {
  const res = await mockLogin(username, password);

  if (res.token) {
    await saveToken(res.token);
  }

  return res;
}

export async function registerVM(
  username: string,
  email: string,
  password: string
) {
  const res = await mockRegister(username, email, password);

  if (res.token) {
    await saveToken(res.token);
  }

  return res;
}