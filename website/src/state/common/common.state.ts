export interface CommonState {
  readonly searchQuery: string;
  readonly showAside: boolean;
  readonly showPromo: boolean;
  readonly showTOC: boolean;
  readonly yScrollPosition: number;
  readonly articleViewportHeight: string;
}

export const initialState: CommonState = {
  searchQuery: "",
  showAside: false,
  showPromo: false,
  showTOC: false,
  yScrollPosition: 0,
  articleViewportHeight: "94vh",
};
