# Warning: this policy exists only for testing purposes.
# How to correctly validate JWT tokens in OPA: https://www.openpolicyagent.org/docs/latest/oauth-oidc/

package graphql.authz.has_age_defined

import input.request

default allow = { "allow" : false }

valid_jwt := [is_valid, claims] if {
  token := replace(request.headers["Authorization"][0], "Bearer ", "")
  claims := io.jwt.decode(token)[1]

  exts := object.get(input, "extensions", {})
  secret := object.get(exts, "secret", "")

  is_valid := is_valid_token_or_secret(token, secret)
  is_valid
}

is_valid_token_or_secret(token, secret) if {
   # a toy validation
  checks := { startswith(token, "eyJhbG"),  secret == "secret" } # imitate OR
  checks[true]
}

allow := {"allow": is_valid, "claims": claims } if {
  [is_valid, claims] := valid_jwt
  claims.birthdate
}
