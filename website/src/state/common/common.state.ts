export interface CommonState {
  readonly searchQuery: string;
  readonly showAside: boolean;
  readonly showCookieConsent: boolean;
  readonly showPromo: boolean;
  readonly showTOC: boolean;
  readonly showLegacyDocInfo: boolean;
  readonly yScrollPosition: number;
  readonly articleViewportHeight: string;
}

export const initialState: CommonState = {
  searchQuery: "",
  showAside: false,
  showCookieConsent: false,
  showPromo: false,
  showTOC: false,
  showLegacyDocInfo: false,
  yScrollPosition: 0,
  articleViewportHeight: "94vh",
};
