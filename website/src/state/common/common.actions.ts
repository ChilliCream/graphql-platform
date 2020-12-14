import { createAction } from "../state.helpers";

export const changeSearchQuery = createAction<{ readonly query: string }>(
  "[Common] Change search query"
);

export const closeAside = createAction("[Common] Close aside pane");

export const closeTOC = createAction("[Common] Close table of contents pane");

export const expandNavigationGroup = createAction<{ readonly path: string }>(
  "[Common] Expand navigation group"
);

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

export const hasScrolled = createAction<{readonly yScrollPosition: number}>("[Common] The main view container has scrolled");

export const setArticleHeight = createAction<{readonly articleHeight: string}>("[Common] The height of the lastly displayed article.");
