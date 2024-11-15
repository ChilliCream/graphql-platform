import { createAction } from "@/state/state.helpers";

export const changeSearchQuery = createAction<{ readonly query: string }>(
  "[Common] Change search query"
);

export const closeAside = createAction("[Common] Close aside pane");

export const closeTOC = createAction("[Common] Close table of contents pane");

export const showPromo = createAction("[Common] Show promo message");

export const hidePromo = createAction("[Common] Hide promo message");

export const toggleAside = createAction("[Common] Toggle aside pane");

export const toggleTOC = createAction("[Common] Toggle table of contents pane");

export const hasScrolled = createAction<{ readonly yScrollPosition: number }>(
  "[Common] The main view container has scrolled"
);

export const setArticleHeight = createAction<{
  readonly articleHeight: string;
}>("[Common] The height of the lastly displayed article.");
