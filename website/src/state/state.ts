import { combineReducers, createStore as createReduxStore } from "redux";
import { navigationReducer } from "./navigation/navigation.reducer";
import { NavigationState } from "./navigation/navigation.state";

export interface State {
  navigation: NavigationState;
}

const rootReducer = combineReducers<State>({
  navigation: navigationReducer,
});

export default function createStore(preloadedState: State) {
  return createReduxStore(rootReducer, preloadedState);
}
