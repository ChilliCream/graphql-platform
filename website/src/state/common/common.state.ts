export interface CommonState {
  expandedPaths: string[];
  searchQuery: string;
  showCookieConsent: boolean;
}

export const initialState: CommonState = {
  expandedPaths: [],
  searchQuery: "",
  showCookieConsent: false,
};
