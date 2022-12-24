# Warning: this policy exists only for testing purposes.
# How to correctly validate JWT tokens in OPA: https://www.openpolicyagent.org/docs/latest/oauth-oidc/

package graphql.authz.has_age_defined

import input.request

default allow = { "allow" : false }

valid_jwt = [is_valid, claims] {
  token := replace(request.headers["Authorization"], "Bearer ", "")
  claims := io.jwt.decode(token)[1]
  is_valid := startswith(token, "eyJhbG") # a toy validation
  is_valid
}

allow = {"allow": is_valid, "claims": claims } {
  [is_valid, claims] := valid_jwt
  claims.birthdate
}
