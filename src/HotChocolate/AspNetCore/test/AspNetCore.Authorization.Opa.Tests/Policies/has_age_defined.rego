# Warning: this policy exists only for testing purposes.
# How to correctly validate JWT tokens in OPA: https://www.openpolicyagent.org/docs/latest/oauth-oidc/

package graphql.authz.has_age_defined

import input.request

default allow = false

valid_jwt = token {
  token := replace(request.headers["Authorization"], "Bearer ", "")
  startswith(token, "eyJhbG") # a toy validation
}

claims = cl {
  cl := io.jwt.decode(valid_jwt)[1]
  valid_jwt
}

allow {
  valid_jwt
  claims.birthdate
}
