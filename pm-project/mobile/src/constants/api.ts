import { Platform } from "react-native";


const defaultHost = Platform.OS === "android" ? "10.0.2.2" : "localhost";

export const API_URL =
	(process.env.REACT_NATIVE_API_URL as string) || `http://${defaultHost}/api`;
