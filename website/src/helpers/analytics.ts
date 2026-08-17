export type AnalyticsEventParameters = Record<string, unknown>;

declare global {
  interface Window {
    Cookiebot?: {
      readonly consent?: {
        readonly statistics?: boolean;
      };
    };
    __gtmLoaded?: boolean;
    gtag?: (...args: unknown[]) => void;
  }
}

/** True only after the visitor has granted analytics consent and GTM started. */
export function canSendAnalytics(): boolean {
  return (
    typeof window !== "undefined" &&
    window.Cookiebot?.consent?.statistics === true &&
    window.__gtmLoaded === true &&
    typeof window.gtag === "function"
  );
}

/**
 * Sends a consent-gated GA4 event. Returns whether the event was queued for
 * GTM, which lets callers avoid delaying navigation when analytics is disabled.
 */
export function sendAnalyticsEvent(
  name: string,
  parameters: AnalyticsEventParameters = {},
): boolean {
  if (!canSendAnalytics()) {
    return false;
  }

  window.gtag!("event", name, parameters);
  return true;
}
