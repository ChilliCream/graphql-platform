import { NextResponse } from "next/server";

// POST /api/contact-sales
// Forwards to HubSpot Forms API when the portal/form env vars are set.
// In local development (or any environment without those vars) we
// console.log the submission and return ok so the form interaction works
// without HubSpot wiring.

const PORTAL_ID = process.env.HUBSPOT_PORTAL_ID;
const FORM_GUID = process.env.HUBSPOT_CONTACT_SALES_FORM_GUID;

interface ContactSalesPayload {
  email?: string;
  company?: string;
  country?: string;
  interest?: string;
  message?: string;
  pageUri?: string;
  website_url_9b3c?: string;
}

export async function POST(req: Request): Promise<NextResponse> {
  let body: ContactSalesPayload;
  try {
    body = (await req.json()) as ContactSalesPayload;
  } catch {
    return NextResponse.json(
      { ok: false, error: "invalid_json" },
      { status: 400 }
    );
  }

  // Honeypot — silently drop bots.
  if (body.website_url_9b3c) {
    return NextResponse.json({ ok: true });
  }

  // Minimal server-side validation.
  if (!body.email || !body.company || !body.country || !body.interest) {
    return NextResponse.json(
      { ok: false, error: "missing_required_fields" },
      { status: 400 }
    );
  }

  if (!PORTAL_ID || !FORM_GUID) {
    // No HubSpot wiring configured. Log the submission so it's visible in
    // dev/preview and tell the client we accepted it. Production deploys
    // must set both env vars.
    // eslint-disable-next-line no-console
    console.log("[contact-sales] submission (no HubSpot configured)", {
      email: body.email,
      company: body.company,
      country: body.country,
      interest: body.interest,
      message: body.message ?? "",
      pageUri: body.pageUri ?? "",
      receivedAt: new Date().toISOString(),
    });
    return NextResponse.json({ ok: true });
  }

  try {
    const r = await fetch(
      `https://api.hsforms.com/submissions/v3/integration/submit/${PORTAL_ID}/${FORM_GUID}`,
      {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          fields: [
            { name: "email", value: body.email },
            { name: "company", value: body.company },
            { name: "country", value: body.country },
            { name: "interest", value: body.interest },
            { name: "message", value: body.message ?? "" },
          ],
          context: {
            pageUri: body.pageUri ?? "",
            pageName: "Contact Sales",
          },
        }),
      }
    );

    if (!r.ok) {
      return NextResponse.json(
        { ok: false, error: "hubspot_rejected" },
        { status: 502 }
      );
    }

    return NextResponse.json({ ok: true });
  } catch {
    return NextResponse.json(
      { ok: false, error: "hubspot_unreachable" },
      { status: 502 }
    );
  }
}
