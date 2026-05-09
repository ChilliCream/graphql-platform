"use client";

import styled from "styled-components";

import { SolutionsRoot } from "../SolutionsRoot";

// SolutionsCinematicRoot extends SolutionsRoot with the chrome the cinematic
// variant adds. The default-variant section components are reused as-is and
// the cinematic chrome (extra gutter, ActLabel chapter markers, the diagram
// connector overlay) is added by the page renderer wrapping each Band in a
// `cc-band-cinematic-wrap` element. Tonal palette, typography, and button
// system stay 1:1 with the default variant by inheriting from SolutionsRoot.
export const SolutionsCinematicRoot = styled(SolutionsRoot)`
  /* ===== Cinematic band wrapper. Each band's <Band> component is wrapped
     in a positioned <div> so the ActLabel chapter marker (rendered as a
     sibling) can be absolutely positioned at top:36px relative to the
     wrapper. The wrapper has block layout so it does not affect the
     band's full-bleed surface. */
  .cc-band-cinematic-wrap {
    position: relative;
    display: block;
  }

  /* ===== Cinematic gutter: every band gets clearance above its heading
     so the ActLabel chapter marker at top:36px sits in the band's gutter
     without overlapping the section heading. The ChapterBand wrapper
     contains a Band whose root element is a section; the cinematic hero
     and architecture sections render their own Band with className
     cc-band so they pick up the same gutter directly. */
  .cc-band-cinematic-wrap > section,
  .cc-band {
    padding-top: clamp(112px, 11vw, 176px);
    overflow: visible;
  }

  /* The legacy in-section .cc-section-label is hidden in the cinematic
     variant: the ActLabel above replaces it as the chapter marker. */
  .cc-section-label {
    display: none;
  }

  /* ===== Diagram band: the architecture band hosts a cinematic overlay
     of anchor and connector-line elements on top of the existing SVG.
     The overlay container needs position:relative so the connector lines
     resolve against the canvas, and overflow:visible so the bezier
     curves are not clipped by the canvas edge. */
  .cc-sl-diagram-canvas {
    position: relative;
    overflow: visible;
  }
  .cc-sl-cin-diagram-overlay {
    position: absolute;
    inset: 0;
    pointer-events: none;
  }

  /* ===== Architecture band hosts the prism language-pill row above the
     diagram heading as a "what's in the supergraph" exhibit. Center the
     row, give it a tight max-width so it reads as a chip exhibit, not a
     full-bleed strip. */
  .cc-sl-cin-language-row {
    margin: 0 auto 28px;
    max-width: 880px;
    display: flex;
    justify-content: center;
  }

  /* ===== Testimonials: the quote body is wrapped in a FrostedExplainer
     plate. The plate sits inline-block by default; reset to block so it
     occupies the testimonial column width, and trim the inner padding so
     the quote does not visually balloon away from the attribution rule. */
  .cc-sl-cin-quote-plate {
    display: block;
    width: 100%;
    margin-bottom: 22px;
  }
  .cc-sl-cin-quote-plate > * {
    max-width: none;
  }
`;
