import { createAction } from "../state.helpers";

export const toggleNavigationGroup = createAction<{ readonly path: string }>(
  "[Navigation] Toggle navigation group"
);
