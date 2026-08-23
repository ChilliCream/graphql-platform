// Pure, filesystem-free half of `YouTubePoster.tsx`'s URL resolution. The
// component itself pulls in `src/image-optimization/manifest.ts` (`node:fs`)
// to resolve a self-hosted optimized poster, which is fine for the server
// components that render it directly but breaks the webpack build the
// moment a client component (`LearnCatalog`, a `"use client"` file) imports
// it: the whole module graph, `node:fs` included, gets pulled into the
// browser bundle. Call sites that only need the raw `i.ytimg.com` URL, and
// that may run from a client component, import from here instead of from
// `YouTubePoster.tsx`, so `getOptimizedImage`'s `node:fs` import never enters
// their bundle. `YouTubePoster.tsx` re-exports these so its existing
// server-side callers are unaffected.

/** 11-character YouTube video id shape (the `v` query param). */
export const YOUTUBE_ID_RE = /^[a-zA-Z0-9_-]{11}$/;

/** The self-hosted image pipeline's lookup key for a video's `maxresdefault` poster; shared by every caller resolving the same remote asset. */
export const youTubePosterKey = (videoId: string): string => `https://i.ytimg.com/vi/${videoId}/maxresdefault.jpg`;

/** External `hqdefault` thumbnail, which always exists (unlike `maxresdefault`, which 404s for many videos). */
export const youTubePosterFallback = (videoId: string): string => `https://i.ytimg.com/vi/${videoId}/hqdefault.jpg`;
