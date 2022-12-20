import React, { FC } from "react";

import { CodeBlock } from "./code-block";
import { InlineCode } from "./inline-code";
import { InputChoiceTabs } from "./input-choice-tabs";
import { Warning } from "./warning";

type Props = {
  readonly packageName: string;
  readonly external?: boolean;
};

export const PackageInstallation: FC<Props> = ({ packageName, external }) => {
  return (
    <>
      <InputChoiceTabs>
        <InputChoiceTabs.CLI>
          <CodeBlock
            language="bash"
            children={`dotnet add package ${packageName}`}
          />
        </InputChoiceTabs.CLI>

        <InputChoiceTabs.VisualStudio>
          <InputChoiceTabs.VisualStudio>
            <p>
              Add the <InlineCode>{packageName}</InlineCode> package using the
              NuGet Package Manager within Visual Studio.
            </p>

            <p>
              <a
                href="https://docs.microsoft.com/nuget/quickstart/install-and-use-a-package-in-visual-studio#nuget-package-manager"
                target="_blank"
              >
                Learn how you can use the NuGet Package Manager to install a
                package
              </a>
            </p>
          </InputChoiceTabs.VisualStudio>
        </InputChoiceTabs.VisualStudio>
      </InputChoiceTabs>

      {!external && (
        <Warning>
          All <InlineCode>HotChocolate.*</InlineCode> packages need to have the
          same version.
        </Warning>
      )}
    </>
  );
};
