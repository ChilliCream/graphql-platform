#!/usr/bin/env bash
exec dotnet "$(dirname "${BASH_SOURCE[0]}")/nitro-aspire-operations.cs" -- "$@"
