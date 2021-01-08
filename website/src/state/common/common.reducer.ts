import { createReducer, onAction } from "../state.helpers";
import { CommonState, initialState } from "./common.state";
import {
  changeSearchQuery,
  closeAside,
  closeTOC,
  expandNavigationGroup,
  hideCookieConsent,
  hideLegacyDocHeader,
  showCookieConsent,
  showLegacyDocInfo,
  toggleAside,
  toggleTOC,
  toggleNavigationGroup,
  hasScrolled,
  setArticleHeight,
} from "./common.actions";

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

  onAction(expandNavigationGroup, (state, { path }) => {
    if (state.expandedPaths.indexOf(path) !== -1) {
      return state;
    }

    const expandedPaths = [...state.expandedPaths, path];

    return {
      ...state,
      expandedPaths,
    };
  }),

  onAction(hideCookieConsent, (state) => ({
    ...state,
    showCookieConsent: false,
  })),

  onAction(hideLegacyDocHeader, (state) => ({
    ...state,
    showLegacyDocInfo: false,
  })),

  onAction(showCookieConsent, (state) => ({
    ...state,
    showCookieConsent: true,
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
    yScrollPosition: yScrollPosition,
  })),

  onAction(setArticleHeight, (state, { articleHeight }) => ({
    ...state,
    articleViewportHeight: articleHeight,
  })),

  onAction(toggleNavigationGroup, (state, { path }) => {
    const expandedPaths = [...state.expandedPaths];
    const index = expandedPaths.indexOf(path);

    if (index !== -1) {
      expandedPaths.splice(index, 1);
    } else {
      expandedPaths.push(path);
    }

    return {
      ...state,
      expandedPaths,
    };
  })
);
