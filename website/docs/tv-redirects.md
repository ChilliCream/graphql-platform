# tv.chillicream.com -> /learn redirect map

Authoritative mapping for retiring tv.chillicream.com (epic website-hnm). This
document is produced by the website repo (website-hnm.4); the redirect rules
themselves are **not** implemented here. The domain redirect executes in the
**cloud repo's ingress**, applied by the user during TV decommission. This
file is the source the user applies from.

Source data: `.nitro/agents/tv-videos-snapshot.json` (the 7-video
`searchYoutubeVideos` GraphQL response captured from tv.chillicream.com),
cross-referenced against the migrated `VideoItem` entries in
`src/data/learn/content.ts`.

## Static routes

| TV route   | Target                     | Notes                                                                                                                                                                                                                    |
| ---------- | -------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `/`        | `/learn`                   | TV's landing page is superseded by the Learn hub.                                                                                                                                                                        |
| `/videos`  | `/learn/browse?type=video` | TV's video listing maps to the Learn browse catalog filtered to videos (matches the video detail page's "Videos" breadcrumb target).                                                                                     |
| `/pricing` | `/learn`                   | The USD 8/month Paddle paywall is dropped by user ruling (epic website-hnm comment, 2026-08-23). There is no paid tier to route to: example downloads are free on `/learn`, so `/pricing` simply lands on the Learn hub. |

No other TV routes (e.g. account/login/checkout pages implied by the former
Paddle paywall) appear in `.nitro/agents/tv-videos-snapshot.json` or anywhere
else in this repo's evidence trail. The snapshot only captures the
`searchYoutubeVideos` query result; it is not a site map. If TV has other
public routes, they are not evidenced here and are out of scope for this doc.

## Video detail routes: `/video/<id>` -> `/learn/videos/<slug>`

All 7 videos from the TV snapshot were migrated into
`src/data/learn/content.ts` (website-hnm.2) and now have native detail pages
at `/learn/videos/[slug]` (website-hnm.1).

**Id-format caveat**: the snapshot exposes two possible identifiers per
video: the GraphQL global id (`WW91dHViZVZpZGVvOi...`, opaque Relay-style
base64, unlikely to appear in a human-facing URL) and the YouTube video id
(11 characters, e.g. `4Mw2A548OGM`). No TV route with a live `/video/<id>`
URL was observed anywhere in this repo's evidence (see grep results below),
so the exact id TV used in that path segment is unconfirmed. This table maps
from the **YouTube id**, the more plausible candidate for a friendly URL
segment; if TV's actual `/video/<id>` used the GraphQL global id instead, the
ingress rule needs the GraphQL id column below as the match key instead.

| YouTube id (assumed TV `<id>`) | GraphQL global id (fallback candidate)     | Title                                                     | Target                                                  |
| ------------------------------ | ------------------------------------------ | --------------------------------------------------------- | ------------------------------------------------------- |
| `4Mw2A548OGM`                  | `WW91dHViZVZpZGVvOiWCo/wdwMVEjLeS9CDRne8=` | How to Use State in DataLoader for Context-Aware Fetching | `/learn/videos/dataloader-state-context-aware-fetching` |
| `dYSqssul4jY`                  | `WW91dHViZVZpZGVvOpG1QzN3jnNEkIs0SqZzqI4=` | Boost GraphQL Performance with EF Core Projections        | `/learn/videos/ef-core-projections-graphql-performance` |
| `DtISlxOBmPQ`                  | `WW91dHViZVZpZGVvOrMoZTyrsJJEusWkX0Kx0Ps=` | Open Telemetry for All Your Services (and More!)          | `/learn/videos/opentelemetry-for-services`              |
| `8TQ2oDUQ1ng`                  | `WW91dHViZVZpZGVvOpJwTEolqTRKv2PhD4eWVw8=` | Offset Pagination is Dead! Meet Relative Cursors          | `/learn/videos/relative-cursors-vs-offset-pagination`   |
| `ZHq1pBjo0Qk`                  | `WW91dHViZVZpZGVvOthvne8u30tIu7FqKuJEWpA=` | Master DataLoader in Layered Architecture!                | `/learn/videos/dataloader-in-layered-architecture`      |
| `FhNK7KMAnXc`                  | `WW91dHViZVZpZGVvOsdW4CGqL/VEl6WMx6shxjM=` | The Future of Data APIs: GreenDonut in Action!            | `/learn/videos/greendonut-in-action`                    |
| `gVIxde5nlWE`                  | `WW91dHViZVZpZGVvOrbN5MHjzzxJlV6oFmqRFQo=` | DataLoader Explained: What, Why & Where It Belongs!       | `/learn/videos/dataloader-explained`                    |

That is all 7 videos hosted on tv.chillicream.com (Feb-May 2025), per the
snapshot and `src/data/learn/content.ts`.

## Implementation note

The rules above describe intent only. The user applies the actual redirect
rules in the **cloud repo's ingress** (source:
`~/code/cloud/src/Cloud/src/ChilliCream.TV.Host`) when tv.chillicream.com is
decommissioned; nothing in the website repo executes a domain-level
redirect. If the `/video/<id>` id-format caveat above cannot be resolved
before decommission, the safest ingress rule is an id-agnostic
`/video/*` -> `/learn/browse?type=video` catch-all rather than guessing
wrong per-video targets.

## Link sweep result

`grep -rn "tv.chillicream.com" app src content` (website repo) returned zero
hits at the time of this doc (2026-08-23): the 7 migrated video descriptions
in `src/data/learn/content.ts` already had their TV self-links stripped
during content migration (website-hnm.2), and no other page under `app/`,
`src/`, or `content/` references the domain. See task website-hnm.4 for
before/after grep output. The only remaining hostname references in the
repo are documentation of this migration itself, in
`docs/design/learn-editorial.md`, out of this task's file scope
(`app`, `src`, `content`).
