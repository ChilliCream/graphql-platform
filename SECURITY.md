# Security Policy

This document is the cybersecurity policy for the open-source projects developed in this repository: Hot Chocolate, Fusion, Strawberry Shake, Green Donut, and the related packages published under the `HotChocolate.*`, `StrawberryShake.*`, `GreenDonut.*`, and `ChilliCream.*` package IDs on NuGet.org.

ChilliCream Inc. supports the development of these projects as an open-source software steward within the meaning of Article 24 of Regulation (EU) 2024/2847 (the Cyber Resilience Act, "CRA"). This policy is maintained to satisfy Article 24(1) and describes how vulnerabilities are documented, remediated, and communicated, and how ChilliCream meets its reporting and cooperation duties under Article 24(2) and 24(3).

The projects are provided under the MIT License, without warranty.

## Reporting a Vulnerability

Please report suspected vulnerabilities privately. Do not open a public issue, discussion, or pull request for anything that might have a security impact.

- Preferred: GitHub private vulnerability reporting at <https://github.com/ChilliCream/graphql-platform/security/advisories/new>
- Alternative: email <contact@chillicream.com> with the subject line `[SECURITY]`

Please include the affected package and version, a description of the issue and its impact, and steps to reproduce or a proof of concept.

We encourage voluntary reporting by researchers, users, and contributors. We will not pursue legal action against anyone who reports in good faith, avoids privacy violations, data destruction, and service disruption, and gives us reasonable time to remediate before disclosing publicly.

## Vulnerability Handling

Reports are handled by the ChilliCream maintainers as follows:

1. The report is acknowledged and triaged privately in a draft GitHub security advisory.
2. If confirmed, the affected versions are identified and documented in the advisory.
3. A fix is developed, tested, and released as a patch version for each supported release line.
4. The advisory is published with a description of the vulnerability, the affected version ranges, and the fixed versions.

Reporters are asked to keep details confidential until the advisory is published. Reporters are credited in the advisory unless they prefer to remain anonymous.

Vulnerabilities in third-party dependencies that affect these packages are handled through the same process. Where a fix requires a dependency or runtime version that a supported release line cannot adopt, the advisory states this and describes the mitigation or the upgrade path.

Response and remediation times depend on maintainer availability and are not guaranteed. Guaranteed response times are available under a commercial support contract.

## Supported Versions

Security fixes are released for the following release lines. Older versions do not receive security fixes and users should upgrade.

| Version | Security fixes |
| ------- | -------------- |
| 16.x    | :white_check_mark: |
| < 16.0  | :x: |

When a new major version is released, the oldest supported line moves out of support. Supported release lines are supported on .NET runtimes that are themselves in support by Microsoft.

## Security Updates

- Security fixes are released as patch versions and are provided free of charge on NuGet.org.
- Security fixes are released to NuGet.org first. The public GitHub security advisory, which describes the vulnerability in detail, follows within 48 hours, once the packages are available on all NuGet mirrors and caches. This delay gives users the chance to update before the details are public. Advisories are published at <https://github.com/ChilliCream/graphql-platform/security/advisories> and syndicated to the GitHub Advisory Database.
- The public advisory follows within 48 hours and reaches dependency scanners such as Dependabot and `dotnet list package --vulnerable`.

## Secure Development

- All changes are made through pull requests and reviewed by a maintainer before merge.
- Every change runs through the continuous integration pipeline, which builds and tests the solution before merge.
- Releases are built and published from the CI pipeline.
- Every release is an immutable GitHub release with the published NuGet packages attached as assets. Once published, neither the tag nor the assets can be changed, and published NuGet package versions cannot be replaced. Users can verify a package from NuGet.org against the corresponding release asset.
- Dependency vulnerability alerts are enabled for this repository and reviewed by the maintainers.
- Members of the ChilliCream GitHub organization are required to use two-factor authentication.

## Regulatory Reporting

In accordance with Article 24(3) of the CRA:

- Where ChilliCream becomes aware of an actively exploited vulnerability in one of these projects, it notifies the competent CSIRT designated as coordinator and ENISA via the single reporting platform, following the timelines of Article 14(2): early warning within 24 hours, notification within 72 hours, and a final report within 14 days after a fix is available.
- Where a severe incident affects the network and information systems ChilliCream provides for the development of these projects (such as this repository or the release pipeline) and has an impact on the security of the projects, ChilliCream notifies the competent CSIRT and ENISA in accordance with Article 14(3) and informs affected users in accordance with Article 14(8).

These obligations apply from 11 September 2026.

## Cooperation with Authorities

ChilliCream cooperates with market surveillance authorities on request to mitigate cybersecurity risks in these projects, and provides this policy and related documentation on reasoned request, as required by Article 24(2) of the CRA.

## Point of Contact

ChilliCream Inc.
Email: <contact@chillicream.com>
Security advisories: <https://github.com/ChilliCream/graphql-platform/security/advisories>

## References

- Regulation (EU) 2024/2847 (Cyber Resilience Act): <https://eur-lex.europa.eu/eli/reg/2024/2847/oj>
- Article 24: Obligations of open-source software stewards
- Article 14: Reporting obligations
- Article 15: Voluntary reporting
