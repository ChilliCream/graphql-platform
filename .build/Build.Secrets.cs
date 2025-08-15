using Nuke.Common;
using Semver;

partial class Build
{
    [Parameter] readonly string NitroApiClientId = "";
    [Parameter] readonly string NitroIdentityClientId = "";
    [Parameter] readonly string NitroIdentityScopes = "";
}
