import { createReducer, onAction } from "@/state/state.helpers";
import {
  changeSearchQuery,
  closeAside,
  closeTOC,
  hasScrolled,
  hidePromo,
  hideCookieConsent,
  hideLegacyDocHeader,
  setArticleHeight,
  showCookieConsent,
  showPromo,
  showLegacyDocInfo,
  toggleAside,
  toggleTOC,
} from "./common.actions";
import { CommonState, initialState } from "./common.state";

export const commonReducer = createReducer<CommonState>(
  initialState,

  onAction(changeSearchQuery, (state, { query }) => {
    return {
      ...state,
      searchQuery: query,
    };
  }),

  onAction(closeAside, (state) => ({
    ...state,
    showAside: false,
  })),

  onAction(closeTOC, (state) => ({
    ...state,
    showTOC: false,
  })),

  onAction(hideLegacyDocHeader, (state) => ({
    ...state,
    showLegacyDocInfo: false,
  })),

  onAction(hideCookieConsent, (state) => ({
    ...state,
    showCookieConsent: false,
  })),

  onAction(showCookieConsent, (state) => ({
    ...state,
    showCookieConsent: true,
  })),

  onAction(showPromo, (state) => ({
    ...state,
    showPromo: true,
  })),

  onAction(hidePromo, (state) => ({
    ...state,
    showPromo: false,
  })),

  onAction(showLegacyDocInfo, (state) => ({
    ...state,
    showLegacyDocInfo: true,
  })),

  onAction(toggleAside, (state) => ({
    ...state,
    showAside: !state.showAside,
    showTOC: false,
  })),

  onAction(toggleTOC, (state) => ({
    ...state,
    showAside: false,
    showTOC: !state.showTOC,
  })),

  onAction(hasScrolled, (state, { yScrollPosition }) => ({
    ...state,
    yScrollPosition,
  })),

  onAction(setArticleHeight, (state, { articleHeight }) => ({
    ...state,
    articleViewportHeight: articleHeight,
  }))
);
