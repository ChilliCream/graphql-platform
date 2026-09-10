export type AnalyticsEventParameters = Record<string, unknown>;

declare global {
  interface Window {
    Cookiebot?: {
      readonly consent?: {
        readonly statistics?: boolean;
      };
    };
    __gtmLoaded?: boolean;
    dataLayer?: unknown[];
    gtag?: (...args: unknown[]) => void;
  }
}

/** True only after the visitor has granted analytics consent and GTM started. */
export function canSendAnalytics(): boolean {
  return (
    typeof window !== "undefined" &&
    window.Cookiebot?.consent?.statistics === true &&
    window.__gtmLoaded === true &&
    Array.isArray(window.dataLayer) &&
    typeof window.gtag === "function"
  );
}

/**
 * Pushes a consent-gated custom event to GTM. Returns whether the event was
 * queued, which lets callers avoid delaying navigation when analytics is
 * disabled.
 */
export function sendAnalyticsEvent(
  name: string,
  parameters: AnalyticsEventParameters = {},
): boolean {
  if (!canSendAnalytics()) {
    return false;
  }

  window.dataLayer!.push({ ...parameters, event: name });
  return true;
}
