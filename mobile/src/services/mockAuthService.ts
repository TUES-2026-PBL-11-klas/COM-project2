// Mock authentication service for development/demo purposes
// This allows the app to work without a backend server

type MockUser = {
  username: string;
  password: string;
  email: string;
  role: "student" | "mentor";
};

const MOCK_USERS: MockUser[] = [
  { username: "demo", password: "demo123", email: "demo@example.com", role: "student" },
  { username: "student", password: "student123", email: "student@example.com", role: "student" },
  { username: "mentor", password: "mentor123", email: "mentor@example.com", role: "mentor" },
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
  return { token, username, email: user.email, role: user.role };
}

export async function mockRegister(
  username: string,
  email: string,
  password: string,
  role: "student" | "mentor"
) {
  // Simulate network delay
  await new Promise((resolve) => setTimeout(resolve, 1000));

  // Check if user already exists
  if (MOCK_USERS.some((u) => u.username === username)) {
    throw new Error("Username already exists");
  }

  // Add new user to mock database
  MOCK_USERS.push({ username, email, password, role });

  // Return a mock token
  const token = `mock_token_${username}_${Date.now()}`;
  return { token, username, email, role };
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

  return { username, email: user.email, role: user.role };
}
