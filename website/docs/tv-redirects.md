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

**Id format confirmed (2026-08-29)**: TV's `/video/<id>` route matches on
the **GraphQL global id** (opaque Relay-style base64, e.g.
`WW91dHViZVZpZGVvOiWCo/wdwMVEjLeS9CDRne8=`), not the YouTube video id.
Evidence, gathered live against tv.chillicream.com:

1. `https://tv.chillicream.com`, `/sitemap.xml`, and `/robots.txt` all return
   HTTP 200 with byte-identical content: the Vite-built React SPA's
   `index.html` shell (`<script type="module" src="/assets/index-O1jnDUdK.js">`).
   There is no real sitemap or robots file, and every path is served the same
   shell, so nothing about the id format is visible without executing the
   bundle's routing logic.
2. The bundle (`/assets/index-O1jnDUdK.js`) defines the route as
   `{id:"video",element:P.jsx(Due,{}),path:"video/:id",loader:({params:e})=>Xn.loadQuery(js,oT,{id:e.id})}`:
   the raw `:id` path param is passed straight through, unmodified, as the
   `id` variable of the compiled `videoPageQuery` Relay request (`oT`).
3. That request resolves `youtubeVideoById(id: $id)` on type `YoutubeVideo`
   and separately selects a `youtubeId` scalar field, i.e. the entity has two
   distinct identifiers and the query argument is named for the first.
4. POSTing that exact persisted query to `https://tv.chillicream.com/graphql`
   (persisted id `779d23eb4f7fddd87aac86d3535f3380`) confirms which one:
   - `variables:{"id":"4Mw2A548OGM"}` (YouTube id) returns
     `{"errors":[{"message":"The node ID string has an invalid format.","path":["youtubeVideoById"]}],"data":{"youtubeVideoById":null}}`.
   - `variables:{"id":"WW91dHViZVZpZGVvOiWCo/wdwMVEjLeS9CDRne8="}` (GraphQL
     global id) succeeds and returns the correct video
     (`"title":"How to Use State in DataLoader for Context-Aware Fetching"`,
     `"youtubeId":"4Mw2A548OGM"`).
5. The video-listing chunk (`/assets/videos-Ccy21ekh.js`) confirms TV's own
   UI links the same way: the video card navigates with
   ``s(`/video/${n.id}`)`` (react-router's `navigate`), where `n.id` comes
   from the `videoCard_video` Relay fragment's `id` scalar field. That
   fragment does not select `youtubeId` at all.

**Caveat**: 2 of the 7 GraphQL global ids below (rows 1 and 6) contain a
literal `/`. TV's own link code does not percent-encode it before calling
`navigate()`, so those 2 videos likely never produced a working `/video/<id>`
URL on the live site itself (the extra `/` splits the pushed path into more
segments than the `video/:id` route matches, so TV's own router would not
match it either). An ingress rule should still match these ids defensively
(both the raw `/` and a `%2F`-encoded form), but if verifying against real
indexed or bookmarked URLs, prioritize the other 5 rows first.

| GraphQL global id (confirmed TV `<id>`)    | YouTube id (informational, from `youtubeId`) | Title                                                     | Target                                                  |
| ------------------------------------------ | -------------------------------------------- | --------------------------------------------------------- | ------------------------------------------------------- |
| `WW91dHViZVZpZGVvOiWCo/wdwMVEjLeS9CDRne8=` | `4Mw2A548OGM`                                | How to Use State in DataLoader for Context-Aware Fetching | `/learn/videos/dataloader-state-context-aware-fetching` |
| `WW91dHViZVZpZGVvOpG1QzN3jnNEkIs0SqZzqI4=` | `dYSqssul4jY`                                | Boost GraphQL Performance with EF Core Projections        | `/learn/videos/ef-core-projections-graphql-performance` |
| `WW91dHViZVZpZGVvOrMoZTyrsJJEusWkX0Kx0Ps=` | `DtISlxOBmPQ`                                | Open Telemetry for All Your Services (and More!)          | `/learn/videos/opentelemetry-for-services`              |
| `WW91dHViZVZpZGVvOpJwTEolqTRKv2PhD4eWVw8=` | `8TQ2oDUQ1ng`                                | Offset Pagination is Dead! Meet Relative Cursors          | `/learn/videos/relative-cursors-vs-offset-pagination`   |
| `WW91dHViZVZpZGVvOthvne8u30tIu7FqKuJEWpA=` | `ZHq1pBjo0Qk`                                | Master DataLoader in Layered Architecture!                | `/learn/videos/dataloader-in-layered-architecture`      |
| `WW91dHViZVZpZGVvOsdW4CGqL/VEl6WMx6shxjM=` | `FhNK7KMAnXc`                                | The Future of Data APIs: GreenDonut in Action!            | `/learn/videos/greendonut-in-action`                    |
| `WW91dHViZVZpZGVvOrbN5MHjzzxJlV6oFmqRFQo=` | `gVIxde5nlWE`                                | DataLoader Explained: What, Why & Where It Belongs!       | `/learn/videos/dataloader-explained`                    |

That is all 7 videos hosted on tv.chillicream.com (Feb-May 2025), per the
snapshot and `src/data/learn/content.ts`.

## Implementation note

The rules above describe intent only. The user applies the actual redirect
rules in the **cloud repo's ingress** (source:
`~/code/cloud/src/Cloud/src/ChilliCream.TV.Host`) when tv.chillicream.com is
decommissioned; nothing in the website repo executes a domain-level
redirect. The `/video/<id>` id format is now confirmed as the GraphQL
global id (see above), so the per-id rows in the table above can be used
directly as the ingress match key. If exact per-id ingress rules turn out to
be impractical (for example because of the embedded `/` characters noted in
the caveat above), an id-agnostic `/video/*` -> `/learn/browse?type=video`
catch-all remains a safe fallback rather than guessing wrong per-video
targets.

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
