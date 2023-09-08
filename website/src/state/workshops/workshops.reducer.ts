import { createReducer } from "@/state/state.helpers";
import { WorkshopsState, initialState } from "./workshops.state";

export const workshopsReducer = createReducer<WorkshopsState>(initialState);
