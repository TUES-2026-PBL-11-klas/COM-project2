import { saveToken, saveRole, saveUsername } from "../../utils/storage";
import { mockLogin, mockRegister } from "../../services/mockAuthService";

// Use mock authentication for development
// To use real backend, change these to loginUser/registerUser from authService
export async function loginVM(username: string, password: string) {
  const res = await mockLogin(username, password);

  if (res.token) {
    await saveToken(res.token);
    await saveRole(res.role);
    await saveUsername(res.username);
  }

  return res;
}

export async function registerVM(
  username: string,
  email: string,
  password: string,
  role: "student" | "mentor"
) {
  const res = await mockRegister(username, email, password, role);

  if (res.token) {
    await saveToken(res.token);
    await saveRole(res.role);
    await saveUsername(res.username);
  }

  return res;
}
