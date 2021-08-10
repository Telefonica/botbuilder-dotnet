#
# Unlists NuGet packages on NuGet.org with the specified version number. Option to unlist all older versions as well.
# Run this first with $unlistPackagesForReal = false (default) to verify what versions will be affected.
# See: https://stackoverflow.com/questions/34958908/where-can-i-find-documentation-for-the-nuget-v3-api
#
param
( 
    [string]$versionToUnlist = "1.0.2",
    [string]$unlistOlderVersionsAlso = "false",
    [string[]]$packageNames = @( "AdaptiveExpressions","Microsoft.Bot.Builder","Microsoft.Bot.Builder.Integration.AspNet.Core" ),
    [string]$nuGetPersonalAccessToken,
    [string]$unlistPackagesForReal = "false"
)

$feedApiUrl = "https://api.nuget.org/v3/index.json";

Function Get-Versions-From-Nuget
{
    param ( [string]$packageName );

    $packageBaseAddress = "https://api.nuget.org/v3-flatcontainer/";
    [string]$versionsUri = $packageBaseAddress + $packageName + "/index.json";

    $response2 = Invoke-RestMethod -Uri $versionsUri

    $versions = $response2.versions;

    return $versions;
}

"versionToUnlist: " + $versionToUnlist;
"unlistOlderVersionsAlso: " + $unlistOlderVersionsAlso;
"unlistPackagesForReal: " + $unlistPackagesForReal;
" ";
"Package versions to unlist:"

foreach ($packageName in $packageNames) {
    $versionsToUnlist = $null;

    $sortedVersions = Get-Versions-From-Nuget $packageName;

    if ($unlistOlderVersionsAlso -eq "true") {
        #Set index to $versionToUnlist
        $index = (0..($sortedVersions.Count-1)) | where {$sortedVersions[$_].StartsWith($versionToUnlist)};

        if ($index -ne $Null) {
            [string[]]$versionsToUnlist = $sortedVersions | select -First ($index[-1] + 1);
        }
    } else {
        [string[]]$versionsToUnlist = $sortedVersions.Where({$_ -eq $versionToUnlist});
    }

    if ($versionsToUnlist.Count -gt 0) {
        "-----------------------------------------";
        $packageName + ":";
        "-----------------------------------------";

        foreach ($version in $versionsToUnlist) {
            if ($unlistPackagesForReal -eq "true") {
                "    Unlisting $version";
                "    nuget delete $packageName $version -Source $feedApiUrl -apikey $nuGetPersonalAccessToken -NonInteractive";
                nuget delete $packageName $version -Source $feedApiUrl -apikey $nuGetPersonalAccessToken -NonInteractive;
            } else {
                "    $version";
            }
        }
    }
}
"-----------------------------------------";
