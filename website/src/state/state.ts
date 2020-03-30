import { combineReducers, createStore as createReduxStore } from "redux";
import { commonReducer as common, CommonState } from "./common";

export interface State {
  common: CommonState;
}

const rootReducer = combineReducers<State>({
  common,
});

export default function createStore(preloadedState: State) {
  return createReduxStore(rootReducer, preloadedState);
}
