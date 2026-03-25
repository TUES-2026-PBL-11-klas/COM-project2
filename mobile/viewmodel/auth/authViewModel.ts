import { loginUser, registerUser } from "../../services/authService";
import { saveToken } from "../../utils/storage";

export async function loginVM(username: string, password: string) {
  const res = await loginUser(username, password);

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
  const res = await registerUser(username, email, password);

  if (res.token) {
    await saveToken(res.token);
  }

  return res;
}