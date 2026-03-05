import { SearchableEntry } from "../utils/searchEngine";
export interface SearchPanelProps {
  searchIndex: SearchableEntry[];
  onResultSelect: (nodeId: string) => void;
}
export declare function SearchPanel({
  searchIndex,
  onResultSelect,
}: SearchPanelProps): import("react/jsx-runtime").JSX.Element;
