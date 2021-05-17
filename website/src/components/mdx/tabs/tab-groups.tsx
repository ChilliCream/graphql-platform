import React, {
  createContext,
  FC,
  useCallback,
  useContext,
  useMemo,
  useState,
} from "react";

type Groups = {
  [group: string]: string;
};

type SetGroup = (groupId: string, value: string) => void;

type TabGroupContextShape = {
  groups: Groups;
  setGroup: SetGroup;
};

const TabGroupContext = createContext<TabGroupContextShape>({
  groups: {},
  setGroup: () => {},
});

const getLocalStorageKey = (groupId: string) => `tab-${groupId}`;

export const TabGroupProvider: FC = ({ children }) => {
  const [groups, setGroups] = useState<Groups>({});

  const handleSetGroup: SetGroup = (groupId, value) => {
    if (groups[groupId] === value) return;

    setGroups({ ...groups, [groupId]: value });

    localStorage.setItem(getLocalStorageKey(groupId), value);
  };

  return (
    <TabGroupContext.Provider value={{ groups, setGroup: handleSetGroup }}>
      {children}
    </TabGroupContext.Provider>
  );
};

type TabGroupReturn = [string, (value: string) => void];

export function useActiveTab(
  defaultValue: string,
  groupId?: string
): TabGroupReturn {
  const { groups, setGroup } = useContext(TabGroupContext);
  const [activeTab, setActiveTab] = useState(defaultValue);

  if (!groupId) return [activeTab, setActiveTab];

  const activeGroupTab = useMemo(() => {
    let value = groups[groupId];

    if (!value) {
      value = localStorage.getItem(getLocalStorageKey(groupId)) ?? defaultValue;
    }

    return value;
  }, [groups]);

  const handleSetGroupTab = useCallback(
    (value: string) => {
      setGroup(groupId, value);
    },
    [groupId]
  );

  return [activeGroupTab, handleSetGroupTab];
}
