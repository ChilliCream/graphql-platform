# Inter font files

`Inter-Regular.ttf` (weight 400) and `Inter-Bold.ttf` (weight 700) are static
subsets of the [Inter](https://github.com/rsms/inter) typeface, sourced from the
[Fontsource](https://fontsource.org/fonts/inter) jsDelivr CDN
(`fonts/inter@latest/latin-{400,700}-normal.ttf`).

Inter is licensed under the SIL Open Font License, Version 1.1
(<https://openfontlicense.org>). These files are vendored so that
`next/og` (`ImageResponse`) can render Open Graph share cards at build time
without any network access.
