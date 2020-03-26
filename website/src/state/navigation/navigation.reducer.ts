import { createReducer, onAction } from "../state.helpers";
import { NavigationState, initialState } from "./navigation.state";
import { toggleNavigationGroup } from "./navigation.actions";

export const navigationReducer = createReducer<NavigationState>(
  initialState,

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
