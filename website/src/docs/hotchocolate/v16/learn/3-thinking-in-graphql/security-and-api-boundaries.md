---
title: "Security and API Boundaries"
description: "Choose public, private, and internal GraphQL boundaries, then align authentication, authorization, operation trust, introspection, and operational limits for Hot Chocolate v16 APIs."
---

A GraphQL API can shift from an internal admin tool to a partner integration with a single routing change. The schema might not change, but the risk profile does.

Begin your security planning by defining the API boundary. Who can access the endpoint? Who controls the clients? Are clients allowed to send any operation document, or only a pre-approved set? Which fields expose sensitive, tenant-specific, or privileged data, or trigger expensive traversals?

This page provides the mental framework for these decisions. For implementation details, see [Securing your API](/docs/hotchocolate/v16/securing-your-api/), [Authentication](/docs/hotchocolate/v16/securing-your-api/authentication/), [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization/), [Request limits](/docs/hotchocolate/v16/securing-your-api/request-limits/), [Cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis/), and [Trusted documents](/docs/hotchocolate/v16/performance/trusted-documents/).

# Start with the API boundary

The first step is not choosing an attribute or option, but identifying the boundary your schema crosses.

Labels like public, private, or internal describe risk models, not value judgments. A field that is safe in a back-office graph may be risky in a partner-facing API. Every field that crosses a boundary becomes part of your product contract, a security review item, and an operational cost surface.

| Boundary | Who owns clients? | Expected operation set | Default posture | Review questions |
| --- | --- | --- | --- | --- |
| Public API | External developers, partners, or callers you do not fully control | Unknown or broad | Strong request limits, cost budgets, pagination limits, explicit rate limiting, documented schema governance, deliberate introspection policy | What can an unknown caller learn, request, or amplify? Which changes break external clients? |
| Private API | A known product, team, partner set, or first-party client fleet | Known or mostly known | Client identity, field authorization, resource limits, operation reporting, and persisted or trusted documents where release workflows support them | Who approves new operations? How do client versions roll out? Which fields need least privilege? |
| Internal API | Controlled services, jobs, admin tools, or back-office workflows | Usually known | Service identity, least privilege, auditability, limits, and transport hardening despite network placement | Which service or person is calling? What happens if the internal network or admin token is compromised? |

During design reviews, summarize your boundary in a short statement:

```text
Public partner API with unknown operation shapes and tenant-scoped data.
```

or:

```text
Private first-party mobile API with known operations and long-lived client versions.
```

This statement should guide your choices for introspection, operation acceptance, batching, limits, cost budgets, rate limiting, logging, and schema evolution. If the API audience changes, revisit your security posture. For more on contract design, see [Schema design principles](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-design-principles/) and [Schema evolution](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-evolution/).

# Authentication identifies the caller

Authentication answers the question: who is making this request?

In ASP.NET Core hosting, identity is usually established before GraphQL execution, through the host pipeline. This identity might come from bearer tokens, cookies, API keys, mTLS, a gateway, or another identity provider. Hot Chocolate uses the authenticated `ClaimsPrincipal` during execution.

Think of authentication as an identity card that travels with the request as the selection set executes. Since a single GraphQL request can include many fields, the identity must be available throughout the operation.

Authentication does not determine whether the caller can read every field or run every mutation. Consider these different callers to the same endpoint:

| Caller | Identity source | GraphQL consequence |
| --- | --- | --- |
| Anonymous visitor | No authenticated principal | Public fields may resolve if the schema allows anonymous access. Protected fields should fail or return no data, depending on authorization and nullability. |
| Signed-in customer | User token or cookie | Viewer fields and customer-owned data can use the caller identity for authorization and data scoping. |
| Administrator | User token with privileged roles or claims | Admin fields still require explicit policies; authentication alone is not approval. |
| Service account | Service token, API key, certificate, or gateway identity | Service-to-service access should use named identity, least privilege, and telemetry. |

Avoid modeling login and logout as GraphQL mutations unless your domain requires schema-level authentication workflows. Most authentication should occur at the host, transport, gateway, or identity-provider boundary. Refer to ASP.NET Core identity guidance and Hot Chocolate's [Authentication](/docs/hotchocolate/v16/securing-your-api/authentication/) documentation for integration details.

Checkpoint: Be able to explain why being "logged in" does not mean "allowed to read this field."

# Authorization belongs at GraphQL boundaries

Authorization answers: is this caller allowed to perform this operation or access this data?

Authorization in GraphQL is not limited to endpoint middleware. A single operation can combine public fields, protected fields, nested objects, global ID lookups, and mutations. Place authorization checks where the graph crosses a permission boundary.

| What you protect | Good boundary | Why |
| --- | --- | --- |
| A shared object type such as `User`, `Organization`, or `Invoice` | Type or object-level policy first | The object may be reachable through many root fields, nested paths, and global ID lookups. |
| A mutation such as `cancelOrder` or `createInvite` | Root field policy | Mutations are operation boundaries with clear intent and side effects. |
| A sensitive field such as `email`, `salary`, `billingInfo`, or `auditLog` | Field-level authorization | The parent object may be visible while one field requires stronger permission. |
| Tenant-owned records | Resolver or data-layer ownership check | The caller may have access to the type but only within a tenant, account, organization, or project. |
| Global ID lookup and node fields | Lookup behavior plus authorization | A forbidden error can reveal that an object exists. Some APIs should return `null` or a not-found shape instead. |
| Admin area | Root grouping, type policies, and field policies | Admin graphs often expose broad data access and need deeper review than read-only customer fields. |

Review object and type access before focusing on path-specific access. For example, `invoice(id:)` is not the only way to reach an `Invoice`; the same object might appear through `customer.invoices`, `organization.search`, or a `node(id:)` field.

Decide how much information to reveal when authorization fails. The [GraphQL response format](https://spec.graphql.org/October2021/#sec-Response-Format) allows partial `data` with `errors`. Hot Chocolate can set a field to `null` and add an error on authorization failure. This affects clients, nullability, and information disclosure. See [Errors](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/errors/) and [Nullability](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/nullability/) for guidance on partial data behavior.

Authorization does not replace business validation or data integrity checks. A caller may be authorized to submit an order, but the domain still decides whether the order can be accepted. Expected domain rejections should be modeled in your schema, often in mutation payload data.

Test authorization with allowed, unauthenticated, and forbidden callers. For guidance on test layering, see [Testing GraphQL APIs](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/testing-graphql-apis/).

# Decide which operation documents you trust

GraphQL servers can accept operation documents in several ways:

| Operation stance | What the server accepts | Choose it when | Trade-off |
| --- | --- | --- | --- |
| Arbitrary documents with limits | Client sends GraphQL operation text at runtime | Public exploration, broad third-party clients, development, or APIs that cannot enumerate operations | Flexible, but every request requires parser, validation, cost, depth, and rate-limit protection. |
| Persisted documents | Client sends a known operation ID when available | You want less document transfer and stable operation lookup, while still allowing a migration path | Improves performance and review, but the posture depends on whether unknown documents may still execute. |
| Trusted documents | Client sends only approved operation IDs | You own or govern clients and can publish operations before release | Strong operation allowlist, but it requires registry, deployment, and client-version coordination. |

Persisted documents reduce repeated document transfer and enable stable operation lookup. Trusted documents go further: the server accepts only an approved set of operations. In trusted-document mode, an unknown document should fail before execution.

Automatic persisted operations are a middle ground. The client first sends a hash. If the server does not recognize it, the client sends the full document so the server can store it. This helps clients adopt persisted operation IDs without a build-time registry workflow, but it is not a strict allowlist.

Consider these questions before choosing an approach:

- Can the server team enumerate the operations clients should send?
- Who approves new operations?
- How are operation IDs published to clients?
- How do mobile or partner client versions roll out?
- What happens when a client sends an unknown document?
- Do variables still require validation, authorization, cost budgets, and page-size limits?

Trusted operations do not replace authentication, authorization, request limits, cost budgets, input validation, tenant isolation, or transport controls. Variables can change cardinality, caller identity affects permissions, and data volume can vary with tenant size.

For operation syntax and request envelopes, see [Operations](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/operations/). For setup and storage, see [Trusted documents](/docs/hotchocolate/v16/performance/trusted-documents/) and [Automatic persisted operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations/). To publish approved operations by client name and version, use the [Nitro client registry](/docs/nitro/apis/client-registry/) and the [Nitro client CLI](/docs/nitro/cli-commands/client/).

# Change the posture by client ownership

Security posture depends on who owns the clients: you, a partner, or the public.

| Client shape | Caller identity | Operation trust | Introspection stance | Limits | Rate limiting | Authorization review depth |
| --- | --- | --- | --- | --- | --- | --- |
| Public web or developer API | User, app, token, or anonymous identity | Arbitrary documents are common, with cost and request limits | Often enabled for discoverability or restricted by policy | Parser, validation, depth, cost, batching, timeout, and page-size limits | Infrastructure and ASP.NET Core policies per caller, token, IP, tenant, or client | Review public fields, protected fields, object access, and existence leaks. |
| Partner API | Partner app identity plus user or tenant context | Approved operation sets are valuable when partners coordinate releases | Usually documented and governed | Strong budgets and compatibility tests | Per partner, app, tenant, and token | Review tenant isolation, audit fields, and support diagnostics. |
| First-party mobile API | User identity plus stable client name and version | Trusted documents work well with release pipelines | Development tooling needs access, production may be restricted | Limits still matter because variables and data cardinality change | Per user, device, client version, tenant, or token | Review long-lived versions and partial rollout behavior. |
| Internal service API | Service identity, workload identity, or gateway identity | Known operations are common | Restrict to tooling and service owners | Keep limits because internal callers can loop, batch, or fan out | Per service, tenant, job, or workload | Review least privilege and auditability for each service account. |
| Admin tool | Named employee or service identity with privileged claims | Known operations are common | Restrict to authenticated tooling | Strong limits and timeouts for broad data access | Per user, role, tenant, or network boundary | Review more deeply than many customer read models because admin fields cross broad data boundaries. |

When you own the clients, you can coordinate generated clients, checked-in operations, trusted documents, and release windows. This provides a stronger operation boundary, but does not remove the need for field authorization or resource limits.

If you do not own the clients, expect unknown operation shapes, slower upgrades, incomplete client telemetry, and potentially hostile traffic. Invest in schema governance, cost analysis, pagination, documented errors, rate limits, and compatibility policy.

See [Clients](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/clients/) for client responsibility and operation contracts. Use [Nitro operation reporting](/docs/nitro/apis/operation-reporting/) to gain visibility into which client versions send which operations before tightening a public, partner, or mobile boundary.

# Keep hardening topics in their own pages

This page covers the mental model for API boundaries. The detailed controls are described in dedicated pages.

| Task | Go here when |
| --- | --- |
| Identify callers | Use [Authentication](/docs/hotchocolate/v16/securing-your-api/authentication/) and the [ASP.NET Core authentication overview](https://learn.microsoft.com/aspnet/core/security/authentication/). |
| Authorize fields, types, and operations | Use [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization/) and the [ASP.NET Core authorization overview](https://learn.microsoft.com/aspnet/core/security/authorization/introduction). |
| Choose introspection policy | Use [Introspection](/docs/hotchocolate/v16/securing-your-api/introspection/) when tooling, schema discovery, or production exposure changes. |
| Bound request shape | Use [Request limits](/docs/hotchocolate/v16/securing-your-api/request-limits/) for parser, validation, depth, batching, document shape, and timeout controls. |
| Set cost budgets | Use [Cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis/) and [Performance mental model](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/performance-mental-model/) for expensive selection sets and page-size budgets. |
| Choose trusted operations | Use [Trusted documents](/docs/hotchocolate/v16/performance/trusted-documents/), [Automatic persisted operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations/), and the [Nitro client registry](/docs/nitro/apis/client-registry/). |
| Review transport exposure | Use [HTTP transport](/docs/hotchocolate/v16/server/http-transport/) for request envelopes, content negotiation, GET and POST behavior, batching, and status-code behavior. |
| Apply rate limiting | Use [ASP.NET Core rate limiting](https://learn.microsoft.com/aspnet/core/performance/rate-limit) and your proxy, gateway, or platform controls. Align the policy with GraphQL batching and cost. |
| Test the guardrails | Use [Testing GraphQL APIs](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/testing-graphql-apis/) for authorization, over-budget, and transport tests. |

Production security also includes logging, audit trails, secrets management, CORS, CSRF, dependency updates, platform hardening, vulnerability response, and incident review. These controls are not specific to GraphQL, but your GraphQL endpoint depends on them.

# Review the security posture before shipping

Use this checklist to review a GraphQL API:

- Name the boundary: public, private, or internal.
- Identify caller types and the identity source for each.
- Mark anonymous, authenticated, privileged, tenant-scoped, and service-only schema areas.
- Review object and type authorization before path-specific checks for objects reachable through several graph paths.
- Review field-level authorization for sensitive fields.
- Review operation boundaries for mutations, admin workflows, and broad exports.
- Decide how forbidden global ID lookups and other sensitive probes respond: forbidden, `null`, not-found, or stripped details.
- Strip fields, error details, object existence signals, and diagnostics that cross the caller's permission boundary.
- Choose between arbitrary documents, persisted documents, or trusted documents.
- Decide introspection policy by environment and audience.
- Set request, depth, cost, timeout, batching, and page-size guardrails as described in the dedicated docs.
- Add rate limiting outside GraphQL where appropriate.
- Add tests for allowed, unauthenticated, forbidden, unknown-document, and over-budget operations.
- Add operation reporting to support incident review, schema governance, rejected unknown documents, and client-version visibility.
- Document ownership, telemetry, incident review, and release process.

The goal is to have one control at each layer: identity, authorization, operation trust, resource limits, transport, and monitoring.

# Troubleshoot boundary gaps

| Symptom | Likely cause | Fix direction |
| --- | --- | --- |
| A protected field returns data to an authenticated caller who should not see it. | Authentication exists, but field-level authorization or ownership checks do not. | Mark the type, field, operation, resolver, or data boundary where the permission belongs. Add allowed, unauthenticated, and forbidden operation tests. |
| A public API allows costly nested operations. | The boundary widened, but request limits, page sizes, cost budgets, or trusted documents were not reviewed. | Use request limits, cost analysis, paging limits, and operation trust. Verify over-budget operations fail before expensive resolver work. |
| Clients cannot discover schema changes during development. | Introspection policy is stricter than the development audience needs, or tooling lacks the right identity. | Separate development, private tooling, and production introspection policy. |
| Trusted documents reject a valid client operation after release. | The operation registry, hash generation, deployment order, or client release process is out of sync. | Confirm the operation ID is present before the client version depends on it. Use the Nitro client registry to inspect the published operation set. |
| Unknown documents appear after switching to trusted documents. | A client version was not registered, a partner still sends arbitrary documents, or rollout order is wrong. | Reject unknown documents in trusted mode, then use operation reporting and client registry data to identify the caller and missing approval. |
| Rate limiting does not stop expensive batched requests. | Per-request infrastructure limits do not match GraphQL batching, operation count, or cost. | Combine rate limiting with batching policy, request limits, and cost analysis so one HTTP request cannot contain more work than the API supports. |
| Internal service callers bypass security review. | The team treated network location as sufficient trust. | Add service identity, least-privilege authorization, request limits, and audit expectations. |

# Next steps

1. Write a one-sentence boundary statement for your API.
2. Mark the schema coordinates that need authorization or information stripping.
3. Choose whether runtime operation text is allowed, persisted, or trusted.
4. Follow the security reference pages for configuration.
5. Add tests that prove the wrong caller and unsafe operation shapes fail before production.
