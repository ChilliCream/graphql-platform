# Warning: this policy exists only for testing purposes.
# How to correctly validate JWT tokens in OPA: https://www.openpolicyagent.org/docs/latest/oauth-oidc/

package graphql.authz.has_age_defined

import input.request

default allow = false

input["token"] = replace(request.headers["Authorization"], "Bearer ", "")

claims := io.jwt.decode(input.token)[1]

allow {
  claims.birthdate
}
