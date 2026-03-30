// Mock authentication service for development/demo purposes
// This allows the app to work without a backend server

const MOCK_USERS = [
  { username: "demo", password: "demo123", email: "demo@example.com" },
  { username: "student", password: "student123", email: "student@example.com" },
];

export async function mockLogin(username: string, password: string) {
  // Simulate network delay
  await new Promise((resolve) => setTimeout(resolve, 1000));

  const user = MOCK_USERS.find(
    (u) => u.username === username && u.password === password
  );

  if (!user) {
    throw new Error("Invalid username or password");
  }

  // Return a mock token
  const token = `mock_token_${username}_${Date.now()}`;
  return { token, username, email: user.email };
}

export async function mockRegister(
  username: string,
  email: string,
  password: string
) {
  // Simulate network delay
  await new Promise((resolve) => setTimeout(resolve, 1000));

  // Check if user already exists
  if (MOCK_USERS.some((u) => u.username === username)) {
    throw new Error("Username already exists");
  }

  // Add new user to mock database
  MOCK_USERS.push({ username, email, password });

  // Return a mock token
  const token = `mock_token_${username}_${Date.now()}`;
  return { token, username, email };
}

export async function mockGetMe(token: string) {
  // Simulate network delay
  await new Promise((resolve) => setTimeout(resolve, 500));

  // Extract username from token (simplified)
  const match = token.match(/mock_token_(\w+)_/);
  if (!match) {
    throw new Error("Invalid token");
  }

  const username = match[1];
  const user = MOCK_USERS.find((u) => u.username === username);

  if (!user) {
    throw new Error("User not found");
  }

  return { username, email: user.email };
}
