# Share-card font files

`Inter-Bold.ttf` (weight 700) is a static subset of the
[Inter](https://github.com/rsms/inter) typeface, sourced from the
[Fontsource](https://fontsource.org/fonts/inter) jsDelivr CDN
(`fonts/inter@latest/latin-700-normal.ttf`).

`JosefinSans-600.woff` (weight 600) is a static subset of the
[Josefin Sans](https://github.com/googlefonts/josefinsans) typeface, from the
same [Fontsource](https://fontsource.org/fonts/josefin-sans) CDN
(`fonts/josefin-sans@latest/latin-600-normal.woff`).

Both typefaces are licensed under the SIL Open Font License, Version 1.1
(<https://openfontlicense.org>). They are vendored so that `next/og`
(`ImageResponse`) can render Open Graph share cards at build time without any
network access, and each file is the one weight its card renders — Josefin Sans
for the `ShareCard` headline, Inter for page titles and `DocsShareCard` lines.
