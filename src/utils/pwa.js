function isHybridHost() {
  if (typeof window === "undefined") {
    return false;
  }

  const userAgent = navigator.userAgent || "";

  // Avoid registering a service worker inside MAUI / WebView hosts.
  return Boolean(window.chrome?.webview) || Boolean(window.HybridWebView) || /\bwv\b/i.test(userAgent);
}

export async function registerPwaIfSupported() {
  if (typeof window === "undefined") {
    return;
  }

  if (!("serviceWorker" in navigator) || !window.isSecureContext || isHybridHost()) {
    return;
  }

  try {
    await navigator.serviceWorker.register("./sw.js");
  } catch (error) {
    console.warn("PWA service worker registration failed.", error);
  }
}
