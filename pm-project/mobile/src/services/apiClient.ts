export async function request(
  endpoint: string,
  method: "GET" | "POST" | "PUT" | "DELETE" | "PATCH",
  body?: any,
  token?: string
) {
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), 10000); // 10s

  try {
    console.log("API Request:", method, endpoint);

    const res = await fetch(endpoint, {
      method,
      headers: {
        "Content-Type": "application/json",
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
      body: body ? JSON.stringify(body) : undefined,
      signal: controller.signal,
    });

    clearTimeout(timeout);

    const text = await res.text();
    const data = text ? JSON.parse(text) : null;

    if (!res.ok) {
      console.error("API Error:", data);
      throw {
        status: res.status,
        message: data || "Request failed",
      };
    }

    return data;
  } catch (err: any) {
    console.error("API Request failed:", err?.message || err);
    throw err;
  }
}