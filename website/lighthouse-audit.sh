#!/bin/bash
# Lighthouse audit script - tests diverse page types
# Outputs JSON reports to /tmp/lighthouse-reports/

export CHROME_PATH=/home/node/.cache/ms-playwright/chromium-1208/chrome-linux64/chrome
REPORT_DIR="/tmp/lighthouse-reports"
rm -rf "$REPORT_DIR"
mkdir -p "$REPORT_DIR"

URLS=(
  "http://localhost:3000/"                                                # Homepage
  "http://localhost:3000/docs/fusion/v15/"                                # Doc landing (images)
  "http://localhost:3000/docs/hotchocolate/v15/fetching-data/filtering"   # Deep doc (code blocks, tabs)
  "http://localhost:3000/docs/nitro/"                                     # Product doc index
  "http://localhost:3000/blog/"                                           # Blog listing
  "http://localhost:3000/blog/2023/08/15/fusion/"                         # Blog article (images)
  "http://localhost:3000/products/nitro/"                                 # Product page
  "http://localhost:3000/pricing/"                                        # Pricing page
)

echo "Running Lighthouse on ${#URLS[@]} pages..."
echo "=========================================="

for url in "${URLS[@]}"; do
  slug=$(echo "$url" | sed 's|http://localhost:3000||;s|/|_|g;s|^_||;s|_$||')
  [ -z "$slug" ] && slug="homepage"

  echo ""
  echo ">>> $url ($slug)"

  npx lighthouse "$url" \
    --output=json \
    --output-path="$REPORT_DIR/$slug.json" \
    --chrome-flags="--headless --no-sandbox --disable-gpu" \
    --only-categories=performance,accessibility,best-practices,seo \
    --quiet 2>/dev/null

  # Extract scores
  if [ -f "$REPORT_DIR/$slug.json" ]; then
    node -e "
      const r = require('$REPORT_DIR/$slug.json');
      const cats = r.categories;
      const scores = Object.entries(cats).map(([k,v]) => k + ': ' + Math.round(v.score*100));
      console.log('  Scores: ' + scores.join(' | '));
    "
  else
    echo "  FAILED"
  fi
done

echo ""
echo "=========================================="
echo ""
echo "FAILING AUDITS SUMMARY:"
echo ""

# Summarize failing audits across all reports
for f in "$REPORT_DIR"/*.json; do
  [ ! -f "$f" ] && continue
  slug=$(basename "$f" .json)
  echo "--- $slug ---"
  node -e "
    const r = require('$f');
    const audits = r.audits;
    const failing = Object.values(audits).filter(a =>
      a.score !== null && a.score < 1 && a.scoreDisplayMode !== 'notApplicable' && a.scoreDisplayMode !== 'informative'
    );
    failing.sort((a,b) => a.score - b.score);
    for (const a of failing.slice(0, 20)) {
      const cat = Object.entries(r.categories).find(([,c]) => c.auditRefs?.some(ref => ref.id === a.id));
      const catName = cat ? cat[0] : '?';
      console.log('  [' + catName + '] ' + a.id + ': ' + Math.round(a.score*100) + ' - ' + (a.title || ''));
    }
  " 2>/dev/null
done
