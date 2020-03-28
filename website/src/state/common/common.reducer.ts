import { createReducer, onAction } from "../state.helpers";
import { CommonState, initialState } from "./common.state";
import {
  hideCookieConsent,
  showCookieConsent,
  toggleNavigationGroup,
} from "./common.actions";

export const commonReducer = createReducer<CommonState>(
  initialState,

  onAction(hideCookieConsent, (state) => ({
    ...state,
    showCookieConsent: false,
  })),

  onAction(showCookieConsent, (state) => ({
    ...state,
    showCookieConsent: true,
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
