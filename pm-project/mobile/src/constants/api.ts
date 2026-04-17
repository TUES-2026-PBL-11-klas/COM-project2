import Constants from "expo-constants";

function getHostForDevice() {
	try {
		const dbg = (Constants.manifest as any)?.debuggerHost || (Constants.expoConfig as any)?.hostUri;
		if (dbg && typeof dbg === "string") {
			const host = dbg.split(":")[0];
			return host;
		}
	} catch (_e) {
		// ignore
	}
	return "localhost";
}

const HOST = getHostForDevice();
export const API_URL = `http://${HOST}:5130/api`;
