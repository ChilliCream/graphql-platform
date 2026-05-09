"use client";

// The cinematic variant of /templates renders the default tree. The only
// cinematic-specific chrome is the `<BlueprintPaper>` backdrop in
// `TemplatesCinematicRoot`; the gallery is 1:1 with the default variant, so
// this module re-exports `TemplatesGrid` under the cinematic name expected
// by the page-component dispatcher.
export { TemplatesGrid as TemplatesCinematicGrid } from "../TemplatesGrid";
