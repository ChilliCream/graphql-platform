export interface CommonState {
  readonly expandedPaths: string[];
  readonly showAside: boolean;
  readonly showCookieConsent: boolean;
  readonly showTOC: boolean;
  readonly showLegacyDocInfo: boolean;
  readonly yScrollPosition: number;
  readonly articleViewportHeight: string;
}

export const initialState: CommonState = {
  expandedPaths: [],
  showAside: false,
  showCookieConsent: false,
  showTOC: false,
  showLegacyDocInfo: false,
  yScrollPosition: 0,
  articleViewportHeight: "94vh",
};
