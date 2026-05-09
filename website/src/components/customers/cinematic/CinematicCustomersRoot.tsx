"use client";

import styled from "styled-components";

import { CustomersRoot } from "../CustomersRoot";

// Cinematic shell for the /customers index. Extends the default
// CustomersRoot tokens, typography, and section CSS verbatim so the
// dark-navy / cream-ink palette stays in lockstep, then layers on the
// cinematic-only adjustments:
//
//   - Bands that host an `<ActLabel>` get 60-70px of extra top padding so
//     the chapter marker has air above the inner content (homepage parity).
//   - The trust-wall band hosts a `<DottedGridBg>` at `z-index: 0`; the
//     wall content lifts to `position: relative; z-index: 1` so the tiles
//     render above the directory grid.
//   - The featured-rail's vibrant tile slots reset link defaults so the
//     `<VibrantTile>` underneath is the only interactive surface.
export const CinematicCustomersRoot = styled(CustomersRoot)`
  /* Chapter-marker air. ActLabel sits absolute at top: 36px; bands need
     enough top padding for the band heading not to collide with it. */
  & > section.cc-band-with-act,
  & > section[aria-label="Featured stories"],
  & > section[aria-label="Trust wall"],
  & > section[aria-label="All stories"],
  & > section[aria-label="Reference call"],
  & > section:first-of-type {
    padding-top: clamp(96px, 9vw, 140px);
  }

  /* Trust wall: the directory grid sits behind the content. Lift the inner
     above the dotted background. */
  .cc-cu-trust-inner-cinematic {
    position: relative;
    z-index: 1;
  }

  /* Vibrant-tile link wrapper for the featured top three. The link is the
     only interactive surface; the tile fills it. */
  .cc-cu-vibrant-link {
    display: block;
    text-decoration: none;
    color: inherit;
    border-radius: 20px;
    outline: none;
  }
  .cc-cu-vibrant-link:focus-visible {
    outline: 2px solid var(--cc-ink);
    outline-offset: 4px;
  }
`;
