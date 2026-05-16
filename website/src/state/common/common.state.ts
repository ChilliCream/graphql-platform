export interface CommonState {
  readonly searchQuery: string;
  readonly showAside: boolean;
  readonly showTOC: boolean;
  readonly yScrollPosition: number;
  readonly articleViewportHeight: string;
}

export const initialState: CommonState = {
  searchQuery: "",
  showAside: false,
  showTOC: false,
  yScrollPosition: 0,
  articleViewportHeight: "94vh",
};
