import { createReducer, onAction } from "../state.helpers";
import { CommonState, initialState } from "./common.state";
import {
  changeSearchQuery,
  closeAside,
  closeTOC,
  hideCookieConsent,
  hideLegacyDocHeader,
  showCookieConsent,
  showLegacyDocInfo,
  toggleAside,
  toggleTOC,
  toggleNavigationGroup,
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

  onAction(toggleNavigationGroup, (state, { path }) => {
    const expandedPaths = [...state.expandedPaths];
    const index = expandedPaths.indexOf(path);

    if (expandedPaths.indexOf(path) !== -1) {
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
