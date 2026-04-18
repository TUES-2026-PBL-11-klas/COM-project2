import { Platform } from "react-native";


const defaultHost = Platform.OS === "android" ? "10.0.2.2" : "localhost";
const port = 5130;

export const API_URL =
	(process.env.EXPO_PUBLIC_API_URL as string) ||
	(process.env.REACT_NATIVE_API_URL as string) ||
	`http://${defaultHost}:${port}/api`;

console.log("[API] Connection String:", API_URL);
