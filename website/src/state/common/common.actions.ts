import { createAction } from "../state.helpers";

export const changeSearchQuery = createAction<{ readonly query: string }>(
  "[Common] Change search query"
);

export const closeAside = createAction("[Common] Close aside pane");

export const closeTOC = createAction("[Common] Close table of contents pane");

export const hideCookieConsent = createAction(
  "[Common] Hide cookie consent message"
);

export const hideLegacyDocHeader = createAction(
  "[Common] Hide legacy documentation message"
);

export const showCookieConsent = createAction(
  "[Common] Show cookie consent message"
);

export const showLegacyDocInfo = createAction(
  "[Common] Show legacy documentation message"
);

export const toggleAside = createAction("[Common] Toggle aside pane");

export const toggleTOC = createAction("[Common] Toggle table of contents pane");

export const toggleNavigationGroup = createAction<{ readonly path: string }>(
  "[Common] Toggle navigation group"
);
