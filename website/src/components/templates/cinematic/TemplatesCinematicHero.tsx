"use client";

// The cinematic variant of /templates renders the default tree. The only
// cinematic-specific chrome is the `<BlueprintPaper>` backdrop in
// `TemplatesCinematicRoot`; the hero, gallery, and CTA strip are 1:1 with
// the default variant, so this module re-exports `TemplatesHero` under the
// cinematic name expected by the page-component dispatcher.
export { TemplatesHero as TemplatesCinematicHero } from "../TemplatesHero";
