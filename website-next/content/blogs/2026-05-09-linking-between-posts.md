---
date: "2026-05-09"
title: "Linking between blog posts"
tags: ["website", "documentation"]
author: ChilliCream
authorUrl: https://chillicream.com
authorImageUrl: https://avatars.githubusercontent.com/u/29404920?s=100&v=4
description: "How cross-references between blog posts are written and resolved on this site."
---

This post exists to demonstrate how blog cross-references work in our docs system. The links below are written as plain relative markdown paths — they remain clickable on GitHub or in any raw markdown viewer, and the build-time remark plugin rewrites them to the canonical `/blogs/YYYY/MM/DD/slug` URLs when the page is rendered.

## Same-folder reference (loose file)

Both this post and the target are loose files at the root of `blogs/`, so the relative path is simply the target's filename:

- [Hot Chocolate 15](./2025-02-01-hot-chocolate-15/2025-02-01-hot-chocolate-15.md)
- [Introducing Nitro](./2024-10-07-introducing-nitro/2024-10-07-introducing-nitro.md)

## Folder-based references

When the target post lives in its own folder (so it can colocate images), the path goes through the folder name:

- [Hot Chocolate 14](./2024-08-30-hot-chocolate-14/2024-08-30-hot-chocolate-14.md)
- [Performance Improvements](./2019-05-08-performance.md)

## Deep linking with anchors

Append `#heading-id` to a relative reference to deep-link into a section:

- [Hot Chocolate 15 — what's new](./2025-02-01-hot-chocolate-15/2025-02-01-hot-chocolate-15.md#whats-new)

The plugin keeps the hash fragment intact while rewriting the file part to the canonical URL.

## What the build does

For each `.md`/`.mdx` link the rewrite plugin:

1. Resolves the relative path against the current file's directory.
2. Verifies the target file exists on disk — the build fails with a clear error if it doesn't.
3. Determines whether the target is under `docs/` or `blogs/`.
4. Emits the canonical URL (`/docs/...` for docs, `/blogs/YYYY/MM/DD/slug` for blogs).
5. Preserves the hash fragment.

The result: GitHub-readable source, validated cross-links, and consistent canonical URLs without any manual maintenance.
