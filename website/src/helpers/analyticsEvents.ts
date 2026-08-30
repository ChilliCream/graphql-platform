declare global {
  interface Window {
    gtag?: (...args: unknown[]) => void;
  }
}

/**
 * Every key event (GA4's name for a conversion) this site is allowed to emit,
 * mapped to the parameters it carries. Event names and parameter names are
 * snake_case, as GA4 expects. Nothing outside this map may be sent: a
 * `data-track` value that is not a key here is ignored at runtime.
 *
 * `page_path` is added to every event by `trackEvent` and by the global
 * `data-track` click handler, so it is not listed per event.
 */
export const ANALYTICS_EVENTS = {
  /** The support contact form was submitted successfully. */
  contact_form_submit: ["topic"],
  /** A Nitro app binary was downloaded from the CDN. */
  nitro_download: ["platform", "arch", "channel"],
  /** A link into the hosted Nitro app (sign-up / launch) was clicked. */
  nitro_signup_click: ["location"],
  /** A plan call to action on the pricing surfaces was clicked. */
  pricing_cta_click: ["plan", "location"],
  /** A "talk to sales" / "contact sales" call to action was clicked. */
  contact_sales_click: ["location"],
  /** An outbound link to a source repository was clicked. */
  repo_click: ["repo_url", "item_type", "item_slug"],
  /** The copy button on a template or example CLI command was used. */
  template_cli_copy: ["command_key", "item_slug"],
  /** A click-to-load video facade was activated. */
  video_play: ["video_id", "location"],
  /** A follow / subscribe channel (RSS, YouTube, social) was clicked. */
  subscribe_click: ["channel"],
  /** An outbound link to the merch store was clicked. */
  store_click: ["location"],
  /** A "read the docs" / "get started" call to action was clicked. */
  docs_cta_click: ["location"],
  /** The search modal was opened. */
  search_open: [],
  /** A search result was selected. */
  search_result_click: ["query", "result_url"],
} as const;

export type AnalyticsEventName = keyof typeof ANALYTICS_EVENTS;

/** The exact parameter object a given key event expects. */
export type AnalyticsEventParams<TName extends AnalyticsEventName> = Record<
  (typeof ANALYTICS_EVENTS)[TName][number],
  string
>;

/** A key event together with its parameters, for props that pass one around. */
export type AnalyticsEvent = {
  [TName in AnalyticsEventName]: {
    readonly name: TName;
    readonly params: AnalyticsEventParams<TName>;
  };
}[AnalyticsEventName];

const EVENT_NAMES: readonly string[] = Object.keys(ANALYTICS_EVENTS);

/** Prefix of the HTML attributes that carry event parameters. */
export const TRACK_PARAM_PREFIX = "data-track-";

/** Narrows an arbitrary `data-track` value to a known key event name. */
export function isAnalyticsEventName(value: string | undefined): value is AnalyticsEventName {
  return value !== undefined && EVENT_NAMES.includes(value);
}

/**
 * Sends a key event to GA4 through the `gtag` shim, which is defined before
 * any consent decision as soon as `NEXT_PUBLIC_COOKIEBOT_CBID` is set. Events
 * fired pre-consent queue in `dataLayer` and are replayed by Google Tag
 * Manager once it loads after consent, where GTM's per-tag consent checks
 * gate them; this no-ops only when the Cookiebot id is unset, since then no
 * shim exists at all.
 */
export function trackEvent<TName extends AnalyticsEventName>(name: TName, params: AnalyticsEventParams<TName>): void {
  window.gtag?.("event", name, {
    ...params,
    page_path: window.location.pathname,
  });
}

/**
 * Builds the `data-track` / `data-track-*` attributes for a plain link or
 * button, so the global click handler can emit the event without a React
 * handler. Parameter names are hyphenated in the attribute (`video_id` becomes
 * `data-track-video-id`) and read back as snake_case by `getTrackParams`.
 */
export function trackAttributes(event: AnalyticsEvent): Record<string, string> {
  const attributes: Record<string, string> = { "data-track": event.name };

  for (const [key, value] of Object.entries(event.params as Record<string, string>)) {
    attributes[TRACK_PARAM_PREFIX + key.replace(/_/g, "-")] = value;
  }

  return attributes;
}

/** The shape of a DOM attribute; matches `Attr` structurally. */
export interface TrackAttribute {
  readonly name: string;
  readonly value: string;
}

/**
 * Maps the `data-track-*` attributes of an element to GA4 event parameters.
 * The `data-track` attribute itself (the event name) is not a parameter and is
 * skipped, as is a bare `data-track-` with no parameter name.
 */
export function getTrackParams(attributes: readonly TrackAttribute[]): Record<string, string> {
  const params: Record<string, string> = {};

  for (const { name, value } of attributes) {
    if (!name.startsWith(TRACK_PARAM_PREFIX)) {
      continue;
    }

    const key = name.slice(TRACK_PARAM_PREFIX.length).replace(/-/g, "_");

    if (key) {
      params[key] = value;
    }
  }

  return params;
}
