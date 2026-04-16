import { saveToken, saveUserId, getToken } from "../../utils/storage";
import { loginUser, registerUser, getMe } from "../../services/authService";

export async function loginVM(username: string, password: string) {
  const res = await loginUser(username, password);

  if (res.token) {
    await saveToken(res.token);
    if (res.id) {
      await saveUserId(res.id);
    } else {
      try {
        const me = await getMe(res.token);
        if (me?.id) {
          await saveUserId(me.id);
        }
      } catch { }
    }
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
    if (res.id) {
      await saveUserId(res.id);
    } else {
      try {
        const me = await getMe(res.token);
        if (me?.id) {
          await saveUserId(me.id);
        }
      } catch { }
    }
  }

  return res;
}
