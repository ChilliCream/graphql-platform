import { useLayoutEffect, useRef } from "react";
import { Selector, useStore } from "react-redux";
import { BehaviorSubject, Observable } from "rxjs";

import { State } from "./state";

export function useObservable<TSelection>(
  selector: Selector<State, TSelection>
): Observable<TSelection> {
  const store = useStore<State>();
  const subject = useRef(
    new BehaviorSubject<TSelection>(selector(store.getState()))
  );
  const observable = useRef(subject.current.asObservable());

  useLayoutEffect(() => {
    const unsubscribe = store.subscribe(() => {
      const newState = selector(store.getState());

      if (newState !== subject.current.value) {
        subject.current.next(newState);
      }
    });

    return () => unsubscribe();
  }, [selector, store, subject]);

  return observable.current;
}
