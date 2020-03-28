export interface CommonState {
  expandedPaths: string[];
  showCookieConsent: boolean;
}

export const initialState: CommonState = {
  expandedPaths: [],
  showCookieConsent: false,
};
