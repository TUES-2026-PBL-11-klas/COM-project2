import { useEffect } from "react";
import { useRouter } from "expo-router";
import { getToken } from "../utils/storage";
import RootNavigator from "../view/auth/navigation/RootNavigator";

export default function Index() {
  return <RootNavigator />;
}