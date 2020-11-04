export interface Action<Payload = void> {
  type: string;
  payload: Payload;
}

interface ActionCreator<Payload = void> {
  type: string;
  (payload: Payload): Action<Payload>;
}

export function createAction<Payload = void>(
  type: string
): ActionCreator<Payload> {
  const actionCreator: ActionCreator<Payload> = (payload: Payload) => ({
    type,
    payload,
  });

  actionCreator.type = type;

  return actionCreator;
}

interface ActionReducer<State, Payload = void> {
  type: string;
  reduce: (state: State, payload: Payload) => State;
}

export function onAction<State, Payload = void>(
  action: ActionCreator<Payload>,
  reduce: (state: State, payload: Payload) => State
): ActionReducer<State, Payload> {
  return {
    type: action.type,
    reduce,
  };
}

export function createReducer<State>(
  initialState: State,
  ...reducers: ActionReducer<State, any>[]
) {
  return (state: State = initialState, action: Action<any>) => {
    const reducer = reducers.find((reducer) => reducer.type === action.type);

    if (reducer) {
      return reducer.reduce(state, action.payload);
    }

    return state;
  };
}
