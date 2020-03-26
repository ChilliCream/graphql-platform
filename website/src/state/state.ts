import { combineReducers, createStore as create } from "redux";
import { navigationReducer } from "./navigation/navigation.reducer";
import { NavigationState } from "./navigation/navigation.state";

export interface State {
  navigation: NavigationState;
}

const rootReducer = combineReducers<State>({
  navigation: navigationReducer,
});

export function createStore() {
  const win = window as any;

  return create(
    rootReducer,
    win.__REDUX_DEVTOOLS_EXTENSION__ && win.__REDUX_DEVTOOLS_EXTENSION__()
  );
}
