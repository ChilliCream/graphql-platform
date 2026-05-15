import { Admonition } from "./Admonition";
import { CodeBlock } from "./CodeBlock";
import { InlineCode } from "./InlineCode";
import { InputChoiceTabs } from "./InputChoiceTabs";
import { Link } from "./Link";

type Props = {
  packageName?: string;
  packagename?: string;
  external?: boolean;
};

export function PackageInstallation({
  packageName,
  packagename,
  external,
}: Props) {
  const name = packageName ?? packagename ?? "";

  return (
    <>
      <InputChoiceTabs>
        <InputChoiceTabs.CLI>
          <CodeBlock>
            <code className="language-bash">{`dotnet add package ${name}`}</code>
          </CodeBlock>
        </InputChoiceTabs.CLI>
        <InputChoiceTabs.VisualStudio>
          <p>
            Add the <InlineCode>{name}</InlineCode> package using the NuGet
            Package Manager within Visual Studio.
          </p>
          <p>
            <Link href="https://docs.microsoft.com/nuget/quickstart/install-and-use-a-package-in-visual-studio#nuget-package-manager">
              Learn how you can use the NuGet Package Manager to install a
              package
            </Link>
          </p>
        </InputChoiceTabs.VisualStudio>
      </InputChoiceTabs>
      {!external && (
        <Admonition kind="warning">
          All <InlineCode>HotChocolate.*</InlineCode> packages need to have the
          same version.
        </Admonition>
      )}
    </>
  );
}
