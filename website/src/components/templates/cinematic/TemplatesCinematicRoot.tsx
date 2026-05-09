"use client";

import styled from "styled-components";

import { TemplatesRoot } from "../TemplatesRoot";

// TemplatesCinematicRoot extends TemplatesRoot with the chrome that the
// cinematic variant adds: extra top gutter on the two acts so an `<ActLabel>`
// at top:36px has clearance above the section heading, `position: relative`
// + `overflow: visible` so the absolute chapter marker resolves against the
// section's own box. Everything else inherits from TemplatesRoot so the
// tonal palette, typography, and gallery card styling stay 1:1 with the
// default variant.
export const TemplatesCinematicRoot = styled(TemplatesRoot)`
  /* ===== Cinematic gutter: the two acts (hero, gallery) get clearance
     above their headings so the ActLabel chapter marker at top:36px sits
     in the section gutter without overlapping content. Targets every
     section element Band renders directly inside the cinematic root. */
  /* Match Band-rendered sections directly under the cinematic root. */
  & > .cc-tp-cinematic-band {
    padding-top: clamp(112px, 11vw, 176px);
    position: relative;
    overflow: visible;
  }

  /* The legacy in-section .cc-section-label is hidden in the cinematic
     variant: the new ActLabel sits in the band gutter and replaces it. */
  .cc-section-label {
    display: none;
  }

  /* ===== Featured exhibit (hero InsetWindow) =====
     The InsetWindow primitive owns its own chrome (frosted plate, tab bar,
     viz frame), so the legacy hero two-column grid collapses to a single
     centered column hosting the inset. */
  .cc-tp-cinematic-hero-inner {
    max-width: 1180px;
    margin: 0 auto;
    display: flex;
    flex-direction: column;
    gap: clamp(28px, 4vw, 56px);
  }
  .cc-tp-cinematic-hero-copy {
    display: flex;
    flex-direction: column;
    gap: 18px;
    max-width: 56ch;
  }
  .cc-tp-cinematic-hero-copy h1 {
    font-size: clamp(40px, 6vw, 80px);
    margin: 0;
    line-height: 1.02;
  }
  .cc-tp-cinematic-hero-copy h1 .accent {
    background: var(--cc-accent-gradient);
    -webkit-background-clip: text;
    background-clip: text;
    -webkit-text-fill-color: transparent;
  }
  .cc-tp-cinematic-hero-copy p {
    font-size: clamp(15px, 1.2vw, 19px);
    line-height: 1.55;
    color: var(--cc-ink-dim);
    max-width: 56ch;
    margin: 0;
    text-wrap: pretty;
  }

  /* ===== Featured viz: a stripped-down version of the gallery thumbnail
     so the existing per-template SVG renders centered inside the inset's
     dashed viz frame at exhibit scale. No card chrome around it. */
  .cc-tp-cinematic-featured-viz {
    width: 100%;
    aspect-ratio: 16 / 9;
    display: flex;
    align-items: center;
    justify-content: center;
    background: var(--cc-accent-soft);
    border-radius: 10px;
    overflow: hidden;
    position: relative;
  }
  .cc-tp-cinematic-featured-viz svg {
    width: 100%;
    height: 100%;
    display: block;
  }

  /* ===== Cinematic filter row =====
     The prism-bordered chips replace the flat .cc-tp-filterbar. Each chip
     is wrapped in an unstyled button so URL-driven filter toggles still
     work; the prism chrome lives on the inner span via TerminalChipRow.
     Active state inverts the prism: solid ink fill, no gradient ring. */
  .cc-tp-cinematic-filterbar {
    display: flex;
    flex-wrap: wrap;
    gap: 8px;
    padding-bottom: 18px;
    border-bottom: 1px solid var(--cc-ink-faint);
  }
  .cc-tp-cinematic-filterbar-btn {
    background: transparent;
    border: none;
    padding: 0;
    cursor: pointer;
    border-radius: 12px;
    transition: transform 0.12s ease, opacity 0.15s ease;
  }
  .cc-tp-cinematic-filterbar-btn:hover {
    transform: translateY(-1px);
  }
  .cc-tp-cinematic-filterbar-btn:focus-visible {
    outline: 2px solid var(--cc-ink);
    outline-offset: 2px;
  }
  .cc-tp-cinematic-filterbar-btn.is-inactive > span {
    /* Dim non-active prism chips so the active chip(s) read as the focal
       point of the row; the gradient is preserved but desaturated. */
    opacity: 0.55;
  }
  .cc-tp-cinematic-filterbar-btn.is-inactive:hover > span {
    opacity: 1;
  }
  .cc-tp-cinematic-filterbar-btn.is-active > span {
    /* Active state: solid ink fill, no gradient ring. */
    background-image: linear-gradient(var(--cc-ink), var(--cc-ink)),
      linear-gradient(var(--cc-ink), var(--cc-ink));
    color: #0c1322;
  }
`;
