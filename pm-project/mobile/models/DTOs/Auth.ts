export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  username: string;
  token: string;
}

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
}

export interface RegisterResponse {
  username: string;
  email: string;
  token: string;
}

export interface AuthResponse {
  username: string;
  token: string;
}