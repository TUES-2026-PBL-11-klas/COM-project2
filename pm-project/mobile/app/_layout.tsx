import { Stack } from "expo-router";
import * as Font from "expo-font";
import { Ionicons } from "@expo/vector-icons";
import { useEffect, useState } from "react";
import * as SplashScreen from "expo-splash-screen";

SplashScreen.preventAutoHideAsync();

export default function Layout() {
  const [loaded, error] = Font.useFonts({
    ...Ionicons.font,
  });

  useEffect(() => {
    if (loaded || error) {
      SplashScreen.hideAsync();
    }
  }, [loaded, error]);

  if (!loaded && !error) {
    return null;
  }

  return <Stack screenOptions={{ headerShown: false }} />;
}