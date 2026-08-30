# Analytics: key events

Key event is GA4's name for a conversion: the small set of actions that count
as outcomes. This page lists the ones the site emits, the parameters each one
carries, and the GTM/GA4 configuration a human still has to do.

## How events reach GA4

`src/components/AnalyticsScripts.tsx` loads Cookiebot and, only after the
visitor grants statistics or marketing consent, Google Tag Manager. Until then
`window.gtag` is a `dataLayer.push` shim that queues nothing, so events fired
without consent are dropped. This is intentional and out of scope for changes
here.

`src/helpers/analyticsEvents.ts` is the single source of truth for event names
and parameters:

- `ANALYTICS_EVENTS` maps each event name to its parameter names.
- `trackEvent(name, params)` sends one event; use it where the interaction is
  not a plain link (copy buttons, the video facade, search, the contact form).
- `trackAttributes(event)` returns the `data-track` / `data-track-*` attributes
  for a link or button; the global click handler in `src/components/Analytics.tsx`
  turns them into the event. Prefer this for anything that is an `<a>` or
  `<button>`.
- `getTrackParams` maps the attributes back to parameters:
  `data-track-item-slug` becomes `item_slug`.

Anything not declared in `ANALYTICS_EVENTS` is ignored by the click handler, so
a typo in a `data-track` value sends nothing rather than an unknown event.

Every event additionally carries `page_path`, and events fired through
`data-track` on a link also carry `link_url`.

## The events

| Event                 | Parameters                           | Fires from                                                                           |
| --------------------- | ------------------------------------ | ------------------------------------------------------------------------------------ |
| `contact_form_submit` | `topic`                              | Support contact form, after the endpoint accepts the submission                      |
| `nitro_download`      | `platform`, `arch`, `channel`        | Every download link in `NitroDownload` (split button and the stable/insider matrix)  |
| `nitro_signup_click`  | `location`                           | Links into nitro.chillicream.com and insider.chillicream.com                         |
| `pricing_cta_click`   | `plan`, `location`                   | Plan cards on `/pricing` and the home page selector, including the self-hosted strip |
| `contact_sales_click` | `location`                           | "Talk to sales", "Contact sales", "Talk to us", "Email a trainer"                    |
| `repo_click`          | `repo_url`, `item_type`, `item_slug` | Repository link on a learn template, example, or workshop detail page                |
| `template_cli_copy`   | `command_key`, `item_slug`           | Copy button on a learn detail page's CLI command                                     |
| `video_play`          | `video_id`, `location`               | Click-to-load video facade (`VideoFacade`)                                           |
| `subscribe_click`     | `channel`                            | Footer follow row, the community Slack button, learn subscribe band                  |
| `store_click`         | `location`                           | Footer link to store.chillicream.com                                                 |
| `docs_cta_click`      | `location`                           | "Read the docs" style buttons on marketing pages                                     |
| `search_open`         | none                                 | Opening the DocSearch modal                                                          |
| `search_result_click` | `query`, `result_url`                | Selecting a search result                                                            |

### Parameter values

- `location` is a snake_case page and section slug, for example
  `pricing_hero`, `platform_analytics_closing`, `home_closing`,
  `nitro_download_panel`. Use the existing values as the pattern when adding a
  call to action.
- `plan` is a pricing tier id: `free`, `payg`, `dedicated`, `self`.
- `platform` is `mac`, `windows`, or `linux`; `arch` is `universal`,
  `silicon`, `intel`, `arm64`, `x64`, or `appimage`; `channel` is `stable` or
  `insider`.
- `channel` on `subscribe_click` is the destination: `blog`, `github`, `slack`,
  `youtube`, `x`, `linkedin`, `rss`.
- `item_type` on `repo_click` is the learn content type, for example
  `template` or `example`.

## Adding an event

1. Add the name and its parameter names to `ANALYTICS_EVENTS`.
2. Instrument the element with `trackAttributes` (links and buttons) or
   `trackEvent` (everything else). The `track` prop on `Button`, `Offering`,
   and `NextStepsSection` takes an `AnalyticsEvent` and writes the attributes.
3. Add the event to the table above.
4. Register the event in GTM and GA4, see below.

## What has to be configured in GTM and GA4

The code only emits events into `dataLayer`. Nothing is reported until someone
with access to the container and property does this:

1. **GTM: a GA4 event tag per event name**, or one tag with the event name read
   from a `{{Event}}` style variable. Without a tag, the `dataLayer` push is
   inert.
2. **GTM: data layer variables for every parameter** in the table above, mapped
   as event parameters on the tag with the same snake_case names. Parameters
   that are not mapped never reach GA4.
3. **GTM: consent checks per tag.** The container itself is only injected after
   consent, and nothing on the page rewrites scripts GTM injects, so each tag
   needs its own consent settings.
4. **GA4 admin: custom dimensions** for the parameters that should be usable in
   reports (`location`, `plan`, `platform`, `arch`, `channel`, `topic`,
   `item_type`, `item_slug`, `command_key`, `video_id`, `query`). GA4 only
   surfaces registered event-scoped parameters.
5. **GA4 admin: mark the conversions.** Admin > Events > Mark as key event, for
   the events that count as outcomes, at minimum `contact_form_submit`,
   `nitro_download`, `nitro_signup_click`, `pricing_cta_click`, and
   `contact_sales_click`.
6. **GA4 admin: content groups** are sent with each page view via
   `gtag("set", { content_group })` from `src/components/Analytics.tsx` and
   need no extra setup beyond the GA4 configuration tag being present.

## Verifying locally

`NEXT_PUBLIC_COOKIEBOT_CBID` and `NEXT_PUBLIC_GTM_ID` are unset in local
development, so no consent banner and no tag manager load, and `window.gtag`
does not exist. To check an event, define the same shim the consent script
installs and read `dataLayer` back:

```js
window.dataLayer = [];
window.gtag = function () {
  window.dataLayer.push(arguments);
};
// click the element, then:
window.dataLayer.map((entry) => Array.from(entry));
```

Unit tests for the pure parts (the content group map and the
attribute-to-parameter mapping) run with `yarn test:unit`.
