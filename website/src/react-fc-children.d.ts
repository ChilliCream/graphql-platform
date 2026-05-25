import "react";

declare module "react" {
  interface FunctionComponent<P = {}> {
    (
      props: P & { children?: React.ReactNode },
      context?: any
    ): React.ReactElement<any, any> | null;
  }
}
