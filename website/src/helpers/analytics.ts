export type AnalyticsEventParameters = Record<string, unknown>;

declare global {
  interface Window {
    Cookiebot?: {
      readonly consent?: {
        readonly preferences?: boolean;
        readonly statistics?: boolean;
        readonly marketing?: boolean;
      };
      submitCustomConsent?: (
        preferences: boolean,
        statistics: boolean,
        marketing: boolean,
      ) => void;
    };
    __gtmLoaded?: boolean;
    dataLayer?: unknown[];
    gtag?: (...args: unknown[]) => void;
  }
}

const CONSENT_REQUIRED = Boolean(process.env.NEXT_PUBLIC_COOKIEBOT_CBID);

/**
 * True when third-party embeds that set marketing cookies may load: the visitor
 * granted Cookiebot marketing consent, or no consent manager is configured.
 */
export function canLoadMarketingEmbeds(): boolean {
  return !CONSENT_REQUIRED || window.Cookiebot?.consent?.marketing === true;
}

/**
 * Grants Cookiebot marketing consent and keeps the visitor's other choices.
 * Returns false when Cookiebot has not loaded.
 */
export function grantMarketingConsent(): boolean {
  const cookiebot = window.Cookiebot;
  if (!cookiebot?.submitCustomConsent) {
    return false;
  }

  cookiebot.submitCustomConsent(
    cookiebot.consent?.preferences === true,
    cookiebot.consent?.statistics === true,
    true,
  );
  return true;
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
