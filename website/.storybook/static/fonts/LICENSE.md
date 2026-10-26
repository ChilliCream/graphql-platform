# Josefin Sans (Storybook preview)

`josefin-sans-latin-wght-normal.woff2` is the latin subset of the variable
[Josefin Sans](https://github.com/googlefonts/josefinsans) typeface, sourced
from the [Fontsource](https://fontsource.org/fonts/josefin-sans) jsDelivr CDN
(`@fontsource-variable/josefin-sans@latest/files/josefin-sans-latin-wght-normal.woff2`,
sha256 `21efe1559a026f9ad7f880772917faa3a621186688e8e79e0568e4308c0bafa9`).

Josefin Sans is licensed under the SIL Open Font License, Version 1.1
(<https://openfontlicense.org>).

The site serves the same variable font through `next/font`. Storybook renders
`app/globals.css` without `app/layout.tsx`, so the preview loads it directly and
matches the site across weights 100-700. Satori cannot read WOFF2, so the share
cards use the static faces in `src/og/fonts/`.
