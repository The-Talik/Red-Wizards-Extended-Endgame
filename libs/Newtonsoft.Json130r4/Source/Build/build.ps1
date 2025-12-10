properties {
  $zipFileName = "Json130r4.zip"
  $majorVersion = "13.0"
  $majorWithReleaseVersion = "13.0.4"
  $nugetPrerelease = $null
  $version = GetVersion $majorWithReleaseVersion
  $packageId = "Newtonsoft.Json"
  $signAssemblies = $false
  $signKeyPath = "C:\Development\Releases\newtonsoft.snk"
  $buildDocumentation = $false
  $buildNuGet = $true
  $msbuildVerbosity = 'minimal'
  $treatWarningsAsErrors = $false
  $workingName = if ($workingName) {$workingName} else {"Working"}
  $assemblyVersion = if ($assemblyVersion) {$assemblyVersion} else {$majorVersion + '.0.0'}
  $netCliChannel = "STS"
  $netCliVersion = "9.0.300"
  $nugetUrl = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"
  $ensureNetCliSdk = $true

  $baseDir  = resolve-path ..
  $buildDir = "$baseDir\Build"
  $sourceDir = "$baseDir\Src"
  $docDir = "$baseDir\Doc"
  $releaseDir = "$baseDir\Release"
  $workingDir = "$baseDir\$workingName"

  $nugetPath = "$buildDir\Temp\nuget.exe"
  $vswhereVersion = "3.1.7"
  $vswherePath = "$buildDir\Temp\vswhere.$vswhereVersion"
  $nunitConsoleVersion = "3.8.0"
  $nunitConsolePath = "$buildDir\Temp\NUnit.ConsoleRunner.$nunitConsoleVersion"

  $builds = @(
    @{Framework = "net6.0"; TestsFunction = "NetCliTests"; TestFramework = "net6.0"; Enabled=$true},
    @{Framework = "netstandard2.0"; TestsFunction = "NetCliTests"; TestFramework = "net5.0"; Enabled=$true},
    @{Framework = "netstandard1.3"; TestsFunction = "NetCliTests"; TestFramework = "netcoreapp3.1"; Enabled=$true},
    @{Framework = "netstandard1.0"; TestsFunction = "NetCliTests"; TestFramework = "netcoreapp2.1"; Enabled=$true},
    @{Framework = "net45"; TestsFunction = "NUnitTests"; TestFramework = "net46"; NUnitFramework="net-4.0"; Enabled=$true},
    @{Framework = "net40"; TestsFunction = "NUnitTests"; NUnitFramework="net-4.0"; Enabled=$true},
    @{Framework = "net35"; TestsFunction = "NUnitTests"; NUnitFramework="net-2.0"; Enabled=$true},
    @{Framework = "net20"; TestsFunction = "NUnitTests"; NUnitFramework="net-2.0"; Enabled=$true}
  )
}

framework '4.6x86'

task default -depends Test,Package

# Ensure a clean working directory
task Clean {
  Write-Host "Setting location to $baseDir"
  Set-Location $baseDir

  if (Test-Path -path $workingDir)
  {
    Write-Host "Deleting existing working directory $workingDir"

    Execute-Command -command { del $workingDir -Recurse -Force }
  }

  Write-Host "Creating working directory $workingDir"
  New-Item -Path $workingDir -ItemType Directory

}

# Build each solution, optionally signed
task Build -depends Clean {
  $script:enabledBuilds = $builds | ? {$_.Enabled}
  Write-Host -ForegroundColor Green "Found $($script:enabledBuilds.Length) enabled builds"

  mkdir "$buildDir\Temp" -Force
  
  if ($ensureNetCliSdk)
  {
    EnsureDotNetCli
  }
  EnsureNuGetExists
  EnsureNuGetPackage "vswhere" $vswherePath $vswhereVersion
  EnsureNuGetPackage "NUnit.ConsoleRunner" $nunitConsolePath $nunitConsoleVersion

  $script:msBuildPath = GetMsBuildPath
  Write-Host "MSBuild path $script:msBuildPath"

  NetCliBuild
}

# Optional build documentation, add files to final zip
task Package -depends Build {
  foreach ($build in $script:enabledBuilds)
  {
    $finalDir = $build.Framework

    $sourcePath = "$sourceDir\Newtonsoft.Json\bin\Release\$finalDir"

    if (!(Test-Path -path $sourcePath))
    {
      throw "Could not find $sourcePath"
    }

    robocopy $sourcePath $workingDir\Package\Bin\$finalDir *.dll *.pdb *.xml /NFL /NDL /NJS /NC /NS /NP /XO /XF *.CodeAnalysisLog.xml | Out-Default
  }

  if ($buildNuGet)
  {
    Write-Host -ForegroundColor Green "Copy NuGet package"

    mkdir $workingDir\NuGet
    move -Path $sourceDir\Newtonsoft.Json\bin\Release\*.nupkg -Destination $workingDir\NuGet
    move -Path $sourceDir\Newtonsoft.Json\bin\Release\*.snupkg -Destination $workingDir\NuGet
  }

  Write-Host "Build documentation: $buildDocumentation"

  if ($buildDocumentation)
  {
    $mainBuild = $script:enabledBuilds | where { $_.Framework -eq "net45" } | select -first 1
    $mainBuildFinalDir = $mainBuild.Framework
    $documentationSourcePath = "$workingDir\Package\Bin\$mainBuildFinalDir"
    $docOutputPath = "$workingDir\Documentation\"
    Write-Host -ForegroundColor Green "Building documentation from $documentationSourcePath"
    Write-Host "Documentation output to $docOutputPath"

    # Sandcastle has issues when compiling with .NET 4 MSBuild
    exec { & $script:msBuildPath "/t:Clean;Rebuild" "/v:$msbuildVerbosity" "/p:Configuration=Release" "/p:DocumentationSourcePath=$documentationSourcePath" "/p:OutputPath=$docOutputPath" "/m" "$docDir\doc.shfbproj" | Out-Default } "Error building documentation. Check that you have Sandcastle, Sandcastle Help File Builder and HTML Help Workshop installed."

    move -Path $workingDir\Documentation\LastBuild.log -Destination $workingDir\Documentation.log
  }

  Copy-Item -Path $docDir\readme.txt -Destination $workingDir\Package\
  Copy-Item -Path $docDir\license.txt -Destination $workingDir\Package\

  robocopy $sourceDir $workingDir\Package\Source\Src /MIR /NFL /NDL /NJS /NC /NS /NP /XD bin obj TestResults AppPackages .vs artifacts /XF *.suo *.user *.lock.json | Out-Default
  robocopy $buildDir $workingDir\Package\Source\Build /MIR /NFL /NDL /NJS /NC /NS /NP /XD Temp /XF runbuild.txt | Out-Default
  robocopy $docDir $workingDir\Package\Source\Doc /MIR /NFL /NDL /NJS /NC /NS /NP | Out-Default
  
  # include fuzz tests in ADO pipeline artifacts
  mkdir $workingDir\FuzzTests
  Copy-Item -Path $sourceDir\Newtonsoft.Json.FuzzTests\bin\Release\net6.0\* -Destination $workingDir\FuzzTests

  Compress-Archive -Path $workingDir\Package\* -DestinationPath $workingDir\$zipFileName
}

task Test -depends Build {
  foreach ($build in $script:enabledBuilds)
  {
    Write-Host "Calling $($build.TestsFunction)"
    & $build.TestsFunction $build
  }
}

function NetCliBuild()
{
  $projectPath = "$sourceDir\Newtonsoft.Json.slnx"
  $libraryFrameworks = ($script:enabledBuilds | Select-Object @{Name="Framework";Expression={$_.Framework}} | select -expand Framework) -join ";"
  $testFrameworks = ($script:enabledBuilds | Select-Object @{Name="Resolved";Expression={if ($_.TestFramework -ne $null) { $_.TestFramework } else { $_.Framework }}} | select -expand Resolved) -join ";"

  $additionalConstants = switch($signAssemblies) { $true { "SIGNED" } default { "" } }

  Write-Host -ForegroundColor Green "Restoring packages for $libraryFrameworks in $projectPath"
  Write-Host

  exec { & $script:msBuildPath "/t:restore" "/v:$msbuildVerbosity" "/p:Configuration=Release" "/p:LibraryFrameworks=`"$libraryFrameworks`"" "/p:TestFrameworks=`"$testFrameworks`"" "/m" $projectPath | Out-Default } "Error restoring $projectPath"

  Write-Host -ForegroundColor Green "Building $libraryFrameworks $assemblyVersion in $projectPath"
  Write-Host

  exec { & $script:msBuildPath "/t:build" "/v:$msbuildVerbosity" $projectPath "/p:Configuration=Release" "/p:LibraryFrameworks=`"$libraryFrameworks`"" "/p:TestFrameworks=`"$testFrameworks`"" "/p:AssemblyOriginatorKeyFile=$signKeyPath" "/p:SignAssembly=$signAssemblies" "/p:TreatWarningsAsErrors=$treatWarningsAsErrors" "/p:AdditionalConstants=$additionalConstants" "/p:GeneratePackageOnBuild=$buildNuGet" "/p:ContinuousIntegrationBuild=true" "/p:PackageId=$packageId" "/p:VersionPrefix=$majorWithReleaseVersion" "/p:VersionSuffix=$nugetPrerelease" "/p:AssemblyVersion=$assemblyVersion" "/p:FileVersion=$version" "/m" }
}

function EnsureDotnetCli()
{
  Write-Host "Downloading dotnet-install.ps1"

  # https://stackoverflow.com/questions/36265534/invoke-webrequest-ssl-fails
  [Net.ServicePointManager]::SecurityProtocol = 'TLS12'
  Invoke-WebRequest `
    -Uri "https://dot.net/v1/dotnet-install.ps1" `
    -OutFile "$buildDir\Temp\dotnet-install.ps1"

  exec { & $buildDir\Temp\dotnet-install.ps1 -Channel $netCliChannel -Version $netCliVersion | Out-Default }
  exec { & $buildDir\Temp\dotnet-install.ps1 -Channel $netCliChannel -Version '6.0.400' | Out-Default }
  exec { & $buildDir\Temp\dotnet-install.ps1 -Channel $netCliChannel -Version '3.1.402' | Out-Default }
  exec { & $buildDir\Temp\dotnet-install.ps1 -Channel $netCliChannel -Version '2.1.818' | Out-Default }
}

function EnsureNuGetExists()
{
  if (!(Test-Path $nugetPath))
  {
    Write-Host "Couldn't find nuget.exe. Downloading from $nugetUrl to $nugetPath"
    (New-Object System.Net.WebClient).DownloadFile($nugetUrl, $nugetPath)
  }
}

function EnsureNuGetPackage($packageName, $packagePath, $packageVersion)
{
  if (!(Test-Path $packagePath))
  {
    Write-Host "Couldn't find $packagePath. Downloading with NuGet"
    exec { & $nugetPath install $packageName -OutputDirectory $buildDir\Temp -Version $packageVersion -ConfigFile "$sourceDir\nuget.config" | Out-Default } "Error restoring $packagePath"
  }
}

function GetMsBuildPath()
{
  $path = & $vswherePath\tools\vswhere.exe -latest -prerelease -products * -requires Microsoft.Component.MSBuild -property installationPath
  if (!($path))
  {
    throw "Could not find Visual Studio install path"
  }

  $msBuildPath = join-path $path 'MSBuild\15.0\Bin\MSBuild.exe'
  if (Test-Path $msBuildPath)
  {
    return $msBuildPath
  }

  $msBuildPath = join-path $path 'MSBuild\Current\Bin\MSBuild.exe'
  if (Test-Path $msBuildPath)
  {
    return $msBuildPath
  }

  throw "Could not find MSBuild path"
}

function NetCliTests($build)
{
  $projectPath = "$sourceDir\Newtonsoft.Json.Tests\Newtonsoft.Json.Tests.csproj"
  $location = "$sourceDir\Newtonsoft.Json.Tests"
  $testDir = if ($build.TestFramework -ne $null) { $build.TestFramework } else { $build.Framework }

  try
  {
    Set-Location $location

    exec { dotnet --version | Out-Default }

    Write-Host -ForegroundColor Green "Running tests for $testDir"
    Write-Host "Location: $location"
    Write-Host "Project path: $projectPath"
    Write-Host

    exec { dotnet test $projectPath -f $testDir -c Release -l trx --results-directory $workingDir --no-restore --no-build | Out-Default }
  }
  finally
  {
    Set-Location $baseDir
  }
}

function NUnitTests($build)
{
  $testDir = if ($build.TestFramework -ne $null) { $build.TestFramework } else { $build.Framework }
  $framework = $build.NUnitFramework
  $testRunDir = "$sourceDir\Newtonsoft.Json.Tests\bin\Release\$testDir"

  Write-Host -ForegroundColor Green "Running NUnit tests $testDir"
  Write-Host
  try
  {
    Set-Location $testRunDir
    exec { & $nunitConsolePath\tools\nunit3-console.exe "$testRunDir\Newtonsoft.Json.Tests.dll" --framework=$framework --result=$workingDir\$testDir.xml --out=$workingDir\$testDir.txt | Out-Default } "Error running $testDir tests"
  }
  finally
  {
    Set-Location $baseDir
  }
}

function GetVersion($majorVersion)
{
    $now = [DateTime]::Now

    $year = $now.Year - 2000
    $month = $now.Month
    $totalMonthsSince2000 = ($year * 12) + $month
    $day = $now.Day
    $minor = "{0}{1:00}" -f $totalMonthsSince2000, $day

    $hour = $now.Hour
    $minute = $now.Minute
    $revision = "{0:00}{1:00}" -f $hour, $minute

    return $majorVersion + "." + $minor
}

function Edit-XmlNodes {
    param (
        [xml] $doc,
        [string] $xpath = $(throw "xpath is a required parameter"),
        [string] $value = $(throw "value is a required parameter")
    )

    $nodes = $doc.SelectNodes($xpath)
    $count = $nodes.Count

    Write-Host "Found $count nodes with path '$xpath'"

    foreach ($node in $nodes) {
        if ($node -ne $null) {
            if ($node.NodeType -eq "Element")
            {
                $node.InnerXml = $value
            }
            else
            {
                $node.Value = $value
            }
        }
    }
}

function Execute-Command($command) {
    $currentRetry = 0
    $success = $false
    do {
        try
        {
            & $command
            $success = $true
        }
        catch [System.Exception]
        {
            if ($currentRetry -gt 5) {
                throw $_.Exception.ToString()
            } else {
                write-host "Retry $currentRetry"
                Start-Sleep -s 1
            }
            $currentRetry = $currentRetry + 1
        }
    } while (!$success)
}

# SIG # Begin signature block
# MIIvtgYJKoZIhvcNAQcCoIIvpzCCL6MCAQExDzANBglghkgBZQMEAgEFADB5Bgor
# BgEEAYI3AgEEoGswaTA0BgorBgEEAYI3AgEeMCYCAwEAAAQQH8w7YFlLCE63JNLG
# KX7zUQIBAAIBAAIBAAIBAAIBADAxMA0GCWCGSAFlAwQCAQUABCCgfkCw/ehHgeMn
# Io51PkdbcUy7u3bKy9m3y/PzNBNNC6CCE6YwggVkMIIDTKADAgECAhAGzuExvm1V
# yAf3wMf7ROYgMA0GCSqGSIb3DQEBDAUAMEwxCzAJBgNVBAYTAlVTMRcwFQYDVQQK
# Ew5EaWdpQ2VydCwgSW5jLjEkMCIGA1UEAxMbRGlnaUNlcnQgQ1MgUlNBNDA5NiBS
# b290IEc1MB4XDTIxMDExNTAwMDAwMFoXDTQ2MDExNDIzNTk1OVowTDELMAkGA1UE
# BhMCVVMxFzAVBgNVBAoTDkRpZ2lDZXJ0LCBJbmMuMSQwIgYDVQQDExtEaWdpQ2Vy
# dCBDUyBSU0E0MDk2IFJvb3QgRzUwggIiMA0GCSqGSIb3DQEBAQUAA4ICDwAwggIK
# AoICAQC2M3OA2GIDcBQsERw5XnyufIOGHf4mL0wkrYvqg1+pvD1b/AuYTAJHMOzi
# /uzoNFtmXr871yymJf+MWbPf6tp8KdlGUHIIHW7RGwrdH82ZifoPD3PE4ZwddTLN
# b5faKmqVsmzJCdDqC3t9FwZJme/W3uDIU9SuxnfxhrsjHLjA31n3jn3R74LmJota
# OLX/ddWy2U8J8zeIUNoRpIoUFNFTBAB982pEGP5QcDIHHKiaDjodxQofbgsmabc8
# oldwLIb6TG6VqVhDuawS1v8/7ddDF2tMzp7EkKv/+hBQmqOQV9bnjBCunxYazzUd
# f9d27YqcNacouKddIfwwN93eCBlPFcbnptqQR473lFNMjlMCvv2Z5eqG0K8DAtOb
# qpPxqyiOIAH/TPvMtylA9YekEhMFH0Nu11FQnzi0IO0XCRKPzLkZr5/NvmkR069V
# EG0XhnmWUsayAJ3lrziwNfSIa48OBD187q/N02oQSsbNhsoiPaFKXPsO/4jfXGKn
# wLke2axsfjg3/neTJcKFik+1NwZaBoEU8c6UnZmR6jJazmc9bgRmrQxPLaMu9571
# eJ33Cv1+j+NCilWWvPGfNy38nl+V/owYG/yO/UuQr9cDaBJjrOKTp6LLBOVPZM4D
# +sYUn9mL6MzUYoxr5AAsGZ8aBsYxgVT7UySar1WZup11rrjC3QIDAQABo0IwQDAd
# BgNVHQ4EFgQUaAGTsdJKQEJplEYsHFqIqSW0R08wDgYDVR0PAQH/BAQDAgGGMA8G
# A1UdEwEB/wQFMAMBAf8wDQYJKoZIhvcNAQEMBQADggIBAJL87rgCeRcCUX0hxUln
# p6TxqCQ46wxo6lpCa5z0c8FpSi2zNwVQQpiSngZ5LC4Gmfbv3yugzbOSAYO1oMsn
# tTwjGphJouwtmaVZQ6zSsZPWV9ccvJPWxkDhs28ZVbcT1+VDM6S1q8vawTFkDXTW
# LO3DjW7ru68ZR2FhLcD0BblveNw690JAZVORvZkNk5JUpqk3WSuby5nGvD33BITw
# lDMdD4JaOcsuRcMoGaOym5jI/DFrYI/26YYovOA8fXRdFolbaSTHEIvES7s2T9RZ
# P8OwpJGZ+C7RSgGd9YgS779aEWpZT1lrWmfzj7QTD8DYLz0ocqoZfxF9alufledf
# t5RP8T6hWv8tzJ3fJ3ePMnMcZwp28/pcsb+8Hb0MKJuyxxdnCzMPw7023Pu6Qgur
# 7YTDYtaEFqmxB2upbu7Gz+awRCnC8LNhgCqLb9IUXCWHVGTzpEzBofina+r+6jr8
# edsOj9zG88nUbN7pg6GOHSLsyTqyAHvcO6dCGn/ci6kRPY6nwCBvXQldQ0Tmj2bM
# qVsH8e+beg6zVOGU/Q4sxpPXVf1xmDW4CUr/xikoLPZSLdsUGJIn4hZ+jMrUYb6C
# h5HrmDc/v19ddz80rBs4Q6tocpkyHjoaGaWjOEwj16PnzNUqkheQC1pLvRa9+4Zq
# 4omZ7OSgVRjJowgfE+AyCHLQMIIGkDCCBHigAwIBAgIQCt4y6VCbRKo0sdrxvA7I
# czANBgkqhkiG9w0BAQsFADBMMQswCQYDVQQGEwJVUzEXMBUGA1UEChMORGlnaUNl
# cnQsIEluYy4xJDAiBgNVBAMTG0RpZ2lDZXJ0IENTIFJTQTQwOTYgUm9vdCBHNTAe
# Fw0yMTA3MTUwMDAwMDBaFw0zMTA3MTQyMzU5NTlaMFsxCzAJBgNVBAYTAlVTMRgw
# FgYDVQQKEw8uTkVUIEZvdW5kYXRpb24xMjAwBgNVBAMTKS5ORVQgRm91bmRhdGlv
# biBQcm9qZWN0cyBDb2RlIFNpZ25pbmcgQ0EyMIICIjANBgkqhkiG9w0BAQEFAAOC
# Ag8AMIICCgKCAgEAznkpxwyD+QKdO6sSwPBJD5wtJL9ncyIlhHFM8rWvU5lCCoMp
# 9TcAiXvCIIaJCeOxjXFv0maraGhSr8SANVefC74HBfDTAl/JyoWmOfBxRY/30/0q
# ivfUtoxrw91SR3Gu3eucWxxb4b+hoIpTgbKU+//cnSvi8EmBTk7ntfFkAWw/6Lov
# +nMXU+qEzm/TuCT8qWX2IffLkdXIt4UqQS8Jqjxn7cGLhjqDA9w+5zXpSxSu/JhK
# OecY05XcdGlGnQBPc8RBzUD3ZzXMPoPBSFiH7UZs23iVmVXCJoU9IFaN3WSLD/jZ
# 3TXE8RxJxoY1DODwr4t6kTSQdDPrx3aPrtAcJFblh3JMP0SpZZpV8DHALVZkKKfF
# u2SOL9Wv57MJ6M/mhfyUot2vLVxVlWlplgwOhcHP7a40cVBczF/cAb+IBz+tuB1q
# wGGi4B3qnE2kpYju6xYz75hVcfFqXGmy3+NMZIF6oMJUSLUZmU7HUDCUyMgHt6SP
# 42r7vzRyPJEMXARiNwe5jI6oAWxyeX6dN4ZXiBDa1lVaVuK8yUd7ShbETPbTPaZ5
# BaV/yxcl1rqExPqKzIH+y/a6F33KXSYVGTSFcg/tSEd4vuXbBUuIf2UpPVkK+J2/
# 0J/o8sBSkF3nFZ/USwrvcMKEiINKokHvmivypLkhSfMIEismXSO6rke8ElECAwEA
# AaOCAV0wggFZMBIGA1UdEwEB/wQIMAYBAf8CAQAwHQYDVR0OBBYEFCgOTIkcmZfx
# gfCPCN5XEku8uHjPMB8GA1UdIwQYMBaAFGgBk7HSSkBCaZRGLBxaiKkltEdPMA4G
# A1UdDwEB/wQEAwIBhjATBgNVHSUEDDAKBggrBgEFBQcDAzB5BggrBgEFBQcBAQRt
# MGswJAYIKwYBBQUHMAGGGGh0dHA6Ly9vY3NwLmRpZ2ljZXJ0LmNvbTBDBggrBgEF
# BQcwAoY3aHR0cDovL2NhY2VydHMuZGlnaWNlcnQuY29tL0RpZ2lDZXJ0Q1NSU0E0
# MDk2Um9vdEc1LmNydDBFBgNVHR8EPjA8MDqgOKA2hjRodHRwOi8vY3JsMy5kaWdp
# Y2VydC5jb20vRGlnaUNlcnRDU1JTQTQwOTZSb290RzUuY3JsMBwGA1UdIAQVMBMw
# BwYFZ4EMAQMwCAYGZ4EMAQQBMA0GCSqGSIb3DQEBCwUAA4ICAQA66iJQhuBqWcn+
# rukrkzv8/2CGpyWTqoospvt+Nr2zsgl1xX97v588lngs94uqlTB90YLR4227PZQz
# HQAFEs0K0/UNUfoKPC9DZSn4xJxjPsLaK8N+l4d6oZBAb7AXglpxfk4ocrzM0KWY
# Tnaz3+lt0uGi8VgP8Wggh24FLxzxxiC5SqwZ7wfU7w1g7YugDn6xbcH4+yd6ZpCn
# MDcxYkGKqOtjM7V3bd3Rbl/PDySZ+chy/n6Q6ZNj9Oz/WFYlYOId7CMmI5SzbMWp
# qbdwvPNkrSYwFRtnRV9rwJo/q9jctfwrG9FQBkHMXiLHRQPw4oEoROk0AYCm26zv
# dEqc1pPCIEQHtXyOW+GqX2MORRdiePFfmG8b1xlIw8iBJOorlbEJA6tvOpDTb1I5
# +ph6tM4DwrMrS0LbGPJv0ah9fh26ZPta1xF0KC4/gdyqY7Bmij+X+atdrRZ0jdqc
# SHspWYc9U6SaXWKVXFwvkc0b19dkzECS7ebrPQuC0+euLpvDMzHIafzL21XHB1+g
# ncuwbt7/RknJoDbFKsx5x0qDQ6vfJmrajyNAMd5fGQdgcUHs75G+KWvg7M4RtGRq
# 6NHrXnBg1LHQlRbLDSCXbIoXkywzzksKuxxm9sn2gdz0/v4o4vPQHrxk8Mj6i23U
# 6h97uJQWVPhLWhQtcjc8MJmU2i5xlDCCB6YwggWOoAMCAQICEAwNIqLvZC64iMRP
# oBjwChswDQYJKoZIhvcNAQELBQAwWzELMAkGA1UEBhMCVVMxGDAWBgNVBAoTDy5O
# RVQgRm91bmRhdGlvbjEyMDAGA1UEAxMpLk5FVCBGb3VuZGF0aW9uIFByb2plY3Rz
# IENvZGUgU2lnbmluZyBDQTIwHhcNMjQxMTA0MDAwMDAwWhcNMjcxMTAzMjM1OTU5
# WjCB5TETMBEGCysGAQQBgjc8AgEDEwJVUzEbMBkGCysGAQQBgjc8AgECEwpXYXNo
# aW5ndG9uMR0wGwYDVQQPDBRQcml2YXRlIE9yZ2FuaXphdGlvbjEUMBIGA1UEBRML
# NjAzIDM4OSAwNjgxCzAJBgNVBAYTAlVTMRMwEQYDVQQIEwpXYXNoaW5ndG9uMRAw
# DgYDVQQHEwdSZWRtb25kMSMwIQYDVQQKExpKc29uLk5FVCAoLk5FVCBGb3VuZGF0
# aW9uKTEjMCEGA1UEAxMaSnNvbi5ORVQgKC5ORVQgRm91bmRhdGlvbikwggIiMA0G
# CSqGSIb3DQEBAQUAA4ICDwAwggIKAoICAQCyn0EityVT/CRCGsHJqLeK34VVttIS
# YJUJFj7eWgaL/pDCXhGRTSRd4koEABXviSdzQemsuOIPDuPyrjJUa4dhJ8UaOZQO
# XHIfX2BnFx7HA9TOP/C2YDbIQsksysh2GNa6KMmoQZzIkIXP2RSroDhIsd9eFZUa
# haN720Lwokmy/VEMIexVI/TGsPHQlw0tTtrstCNebn9uBgEbzhpbxaurLRfpIyw9
# rHSzeq8I6QDoFMlf89nquaH9V1PieS8xY1yAZwxIY8yxfqoxBtZC/5+HyMdXQXxI
# 2oRZ7OOkYBswNqe7QyDUQn+1vbQhRr3+Sjc43gttBwDlmKPwur9Vif0W/ETRzC8x
# Frwj43gadWQHrsYcmg2zo+yfp8YZS5bTH+b006lh43ly9+y9YOXcfhaN40jinK2u
# jWJhk7fD4Xa2VGyV5r8Agms0hqUDOromNo0ZblI4TMqiyJRia40nlQAka4HgqpR7
# A5hWGC5TukQvbFUqzbONzCYrVyxc+gu1A9uh41WpzePxVXa2S+0LHnPTP2IstHlG
# 07ixLfaNYVJB7Wqw3wQ74Gywp1r6Zg0MntXZhK/TYsuszBT+TCueUChzStXq+Cpu
# iukj78J1zz7LTQzbOs1qIFo4we/WhDEE+RRt4f4ogQu9nt7UJRX4ZQ/ePjWqQn17
# 2Il0fuM5ArS+hwIDAQABo4IB2TCCAdUwHwYDVR0jBBgwFoAUKA5MiRyZl/GB8I8I
# 3lcSS7y4eM8wHQYDVR0OBBYEFBMUCEWiVUypgQ8exHsjFSaMrW/TMD0GA1UdIAQ2
# MDQwMgYFZ4EMAQMwKTAnBggrBgEFBQcCARYbaHR0cDovL3d3dy5kaWdpY2VydC5j
# b20vQ1BTMA4GA1UdDwEB/wQEAwIHgDATBgNVHSUEDDAKBggrBgEFBQcDAzCBmwYD
# VR0fBIGTMIGQMEagRKBChkBodHRwOi8vY3JsMy5kaWdpY2VydC5jb20vTkVURm91
# bmRhdGlvblByb2plY3RzQ29kZVNpZ25pbmdDQTIuY3JsMEagRKBChkBodHRwOi8v
# Y3JsNC5kaWdpY2VydC5jb20vTkVURm91bmRhdGlvblByb2plY3RzQ29kZVNpZ25p
# bmdDQTIuY3JsMIGFBggrBgEFBQcBAQR5MHcwJAYIKwYBBQUHMAGGGGh0dHA6Ly9v
# Y3NwLmRpZ2ljZXJ0LmNvbTBPBggrBgEFBQcwAoZDaHR0cDovL2NhY2VydHMuZGln
# aWNlcnQuY29tL05FVEZvdW5kYXRpb25Qcm9qZWN0c0NvZGVTaWduaW5nQ0EyLmNy
# dDAJBgNVHRMEAjAAMA0GCSqGSIb3DQEBCwUAA4ICAQDLb+WYnliLl0sF+vux1FCn
# 08ZiVqUpViClF8q7uC+2IdrA2h2BUpxtFhp728A28O445z/9VakvSQJIwm0tWDJP
# 0mv40JamjQe2IyAMpbZB2KKomovaa0FxW56vINPFtmDK5bSB5m8qbdx/3TR/jyUz
# 50Pmdgs4uSAwYxYZPYBo4PRunsPUCLksNPmFtE8udlN6b4608egtKyL7ajvCsA7A
# 2sQucY7hVzE6gY1EtoA3gYD/THsWoK0a1zARscamio8b+oHkUjzmQM2vI5Vk6A+v
# 9ckTFP5oXBinIzZZ0UTD/5OPd7LXc3vgT1qMhNjsf4N9MrrIHN2e78zMQZD4T/cy
# VNoPQcbxe6XXjCTImROkIZI0lRedLf0WpkUrvcVJxyTnfDs40Q4e9Y5HfiaJpPH7
# UgdlLRDhCBKkGbnVSb3s6bvD0ljom5PKn/n6Wzs2jFkopV7M3aaEzLSwaC/2TVav
# FQjuAfoBrmEAOXOeezyk1/0uooHsVZ1svHUg0Wkuqq1VGe/dHnF2ULDq3QqzAgne
# ei6hByq267+SrXhJNZru5MT7PYosLQT0wJ8U+jV3gdGOMH13GYghKN0h9YrVdJ1L
# EbdZ1TzpGd5KYLO3Dhi87w+XuUgM/Kc5xu2rSPNITpctQtTHPyb++CoK7yDSD3vv
# f5m9j0BmTNQhwMTQ7sq0jjGCG2YwghtiAgEBMG8wWzELMAkGA1UEBhMCVVMxGDAW
# BgNVBAoTDy5ORVQgRm91bmRhdGlvbjEyMDAGA1UEAxMpLk5FVCBGb3VuZGF0aW9u
# IFByb2plY3RzIENvZGUgU2lnbmluZyBDQTICEAwNIqLvZC64iMRPoBjwChswDQYJ
# YIZIAWUDBAIBBQCggbQwGQYJKoZIhvcNAQkDMQwGCisGAQQBgjcCAQQwHAYKKwYB
# BAGCNwIBCzEOMAwGCisGAQQBgjcCARUwLwYJKoZIhvcNAQkEMSIEIO80Uq2OqATv
# viKfA9h0t+JgCsKNze7DVKLjDuToaYwtMEgGCisGAQQBgjcCAQwxOjA4oBKAEABK
# AHMAbwBuAC4ATgBFAFShIoAgaHR0cHM6Ly93d3cubmV3dG9uc29mdC5jb20vanNv
# biAwDQYJKoZIhvcNAQEBBQAEggIAPPVO6T2BmKzcYLQvEsk6gMRkJ0S+YgXf2yca
# d/3S9oUgT1AYn9QvaElO1wiIPSBw/F2hYanWiFOVSz1BAu3O0fsJ7OYCyH3GVgIA
# RaudfCJj1YGfUpmVT/2lev6wYlPWbBFX9ocGmkhMPiSxkSXkkTxMhR9jOAzUUVAp
# LsK/+yFb5zN7dgQA1JEy+el+M+g9/FTOiGHMyNA/FnAkgThvIbha5EsioMsMIVrR
# thyryiaKfjuT/i+NWf7YXuII086WnYpp5nc6Ml2f+zrhuRPkjG4X+fXzpqX8EFlP
# 4mzDvla33BnSwhS9Gs0nS85BuS8VMUJr6VmK8WTOK1uMJyf7dn4G83bhwVXC2ceE
# EUe5MnMkPdhvSPOQNyPEPxi7Y8wfcU5cUD3yFX1D1+3Plfj335RCOqpTvOuU1A9m
# 8DYENCiIeIXEgmH4eE/+s50AmWebracI8Ijqw4m/Xw0LxQEtwjCn3E8vMg8esVvQ
# dqJFLSkyyPeV642HXPSVEAsrlkEpeazHFntQDCKchtDtwy1FMeU0uiv6pY0kFyjg
# Qx/CbB4nKZOjQK3KhCduAC8BIsqtTR48TsKoZxe002VymrGbXCuUZmqAr3WAcbz5
# I15fol0AMQjeS5+evUQtmMHd5hrrHnj8dBaJDr0xX7jVkJAq+ROEtIcypwk8VpK+
# eZM7HjahghgRMIIYDQYKKwYBBAGCNwMDATGCF/0wghf5BgkqhkiG9w0BBwKgghfq
# MIIX5gIBAzEPMA0GCWCGSAFlAwQCAQUAMIIBYgYLKoZIhvcNAQkQAQSgggFRBIIB
# TTCCAUkCAQEGCisGAQQBhFkKAwEwMTANBglghkgBZQMEAgEFAAQggMf3KoNIGkFY
# DD/IOA1hNLbtp9T2jL4Cn52AoZ9z8tQCBmjB+9NdgRgTMjAyNTA5MTYwODA0MzUu
# MjI1WjAEgAIB9KCB4aSB3jCB2zELMAkGA1UEBhMCVVMxEzARBgNVBAgTCldhc2hp
# bmd0b24xEDAOBgNVBAcTB1JlZG1vbmQxHjAcBgNVBAoTFU1pY3Jvc29mdCBDb3Jw
# b3JhdGlvbjElMCMGA1UECxMcTWljcm9zb2Z0IEFtZXJpY2EgT3BlcmF0aW9uczEn
# MCUGA1UECxMeblNoaWVsZCBUU1MgRVNOOjc4MDAtMDVFMC1EOTQ3MTUwMwYDVQQD
# EyxNaWNyb3NvZnQgUHVibGljIFJTQSBUaW1lIFN0YW1waW5nIEF1dGhvcml0eaCC
# DyEwggeCMIIFaqADAgECAhMzAAAABeXPD/9mLsmHAAAAAAAFMA0GCSqGSIb3DQEB
# DAUAMHcxCzAJBgNVBAYTAlVTMR4wHAYDVQQKExVNaWNyb3NvZnQgQ29ycG9yYXRp
# b24xSDBGBgNVBAMTP01pY3Jvc29mdCBJZGVudGl0eSBWZXJpZmljYXRpb24gUm9v
# dCBDZXJ0aWZpY2F0ZSBBdXRob3JpdHkgMjAyMDAeFw0yMDExMTkyMDMyMzFaFw0z
# NTExMTkyMDQyMzFaMGExCzAJBgNVBAYTAlVTMR4wHAYDVQQKExVNaWNyb3NvZnQg
# Q29ycG9yYXRpb24xMjAwBgNVBAMTKU1pY3Jvc29mdCBQdWJsaWMgUlNBIFRpbWVz
# dGFtcGluZyBDQSAyMDIwMIICIjANBgkqhkiG9w0BAQEFAAOCAg8AMIICCgKCAgEA
# nnznUmP94MWfBX1jtQYioxwe1+eXM9ETBb1lRkd3kcFdcG9/sqtDlwxKoVIcaqDb
# +omFio5DHC4RBcbyQHjXCwMk/l3TOYtgoBjxnG/eViS4sOx8y4gSq8Zg49REAf5h
# uXhIkQRKe3Qxs8Sgp02KHAznEa/Ssah8nWo5hJM1xznkRsFPu6rfDHeZeG1Wa1wI
# SvlkpOQooTULFm809Z0ZYlQ8Lp7i5F9YciFlyAKwn6yjN/kR4fkquUWfGmMopNq/
# B8U/pdoZkZZQbxNlqJOiBGgCWpx69uKqKhTPVi3gVErnc/qi+dR8A2MiAz0kN0nh
# 7SqINGbmw5OIRC0EsZ31WF3Uxp3GgZwetEKxLms73KG/Z+MkeuaVDQQheangOEMG
# J4pQZH55ngI0Tdy1bi69INBV5Kn2HVJo9XxRYR/JPGAaM6xGl57Ei95HUw9NV/uC
# 3yFjrhc087qLJQawSC3xzY/EXzsT4I7sDbxOmM2rl4uKK6eEpurRduOQ2hTkmG1h
# SuWYBunFGNv21Kt4N20AKmbeuSnGnsBCd2cjRKG79+TX+sTehawOoxfeOO/jR7wo
# 3liwkGdzPJYHgnJ54UxbckF914AqHOiEV7xTnD1a69w/UTxwjEugpIPMIIE67SFZ
# 2PMo27xjlLAHWW3l1CEAFjLNHd3EQ79PUr8FUXetXr0CAwEAAaOCAhswggIXMA4G
# A1UdDwEB/wQEAwIBhjAQBgkrBgEEAYI3FQEEAwIBADAdBgNVHQ4EFgQUa2koOjUv
# SGNAz3vYr0npPtk92yEwVAYDVR0gBE0wSzBJBgRVHSAAMEEwPwYIKwYBBQUHAgEW
# M2h0dHA6Ly93d3cubWljcm9zb2Z0LmNvbS9wa2lvcHMvRG9jcy9SZXBvc2l0b3J5
# Lmh0bTATBgNVHSUEDDAKBggrBgEFBQcDCDAZBgkrBgEEAYI3FAIEDB4KAFMAdQBi
# AEMAQTAPBgNVHRMBAf8EBTADAQH/MB8GA1UdIwQYMBaAFMh+0mqFKhvKGZgEByfP
# UBBPaKiiMIGEBgNVHR8EfTB7MHmgd6B1hnNodHRwOi8vd3d3Lm1pY3Jvc29mdC5j
# b20vcGtpb3BzL2NybC9NaWNyb3NvZnQlMjBJZGVudGl0eSUyMFZlcmlmaWNhdGlv
# biUyMFJvb3QlMjBDZXJ0aWZpY2F0ZSUyMEF1dGhvcml0eSUyMDIwMjAuY3JsMIGU
# BggrBgEFBQcBAQSBhzCBhDCBgQYIKwYBBQUHMAKGdWh0dHA6Ly93d3cubWljcm9z
# b2Z0LmNvbS9wa2lvcHMvY2VydHMvTWljcm9zb2Z0JTIwSWRlbnRpdHklMjBWZXJp
# ZmljYXRpb24lMjBSb290JTIwQ2VydGlmaWNhdGUlMjBBdXRob3JpdHklMjAyMDIw
# LmNydDANBgkqhkiG9w0BAQwFAAOCAgEAX4h2x35ttVoVdedMeGj6TuHYRJklFaW4
# sTQ5r+k77iB79cSLNe+GzRjv4pVjJviceW6AF6ycWoEYR0LYhaa0ozJLU5Yi+LCm
# crdovkl53DNt4EXs87KDogYb9eGEndSpZ5ZM74LNvVzY0/nPISHz0Xva71QjD4h+
# 8z2XMOZzY7YQ0Psw+etyNZ1CesufU211rLslLKsO8F2aBs2cIo1k+aHOhrw9xw6J
# CWONNboZ497mwYW5EfN0W3zL5s3ad4Xtm7yFM7Ujrhc0aqy3xL7D5FR2J7x9cLWM
# q7eb0oYioXhqV2tgFqbKHeDick+P8tHYIFovIP7YG4ZkJWag1H91KlELGWi3SLv1
# 0o4KGag42pswjybTi4toQcC/irAodDW8HNtX+cbz0sMptFJK+KObAnDFHEsukxD+
# 7jFfEV9Hh/+CSxKRsmnuiovCWIOb+H7DRon9TlxydiFhvu88o0w35JkNbJxTk4Mh
# F/KgaXn0GxdH8elEa2Imq45gaa8D+mTm8LWVydt4ytxYP/bqjN49D9NZ81coE6aQ
# Wm88TwIf4R4YZbOpMKN0CyejaPNN41LGXHeCUMYmBx3PkP8ADHD1J2Cr/6tjuOOC
# ztfp+o9Nc+ZoIAkpUcA/X2gSMkgHAPUvIdtoSAHEUKiBhI6JQivRepyvWcl+JYbY
# bBh7pmgAXVswggeXMIIFf6ADAgECAhMzAAAATBtLnGPC5NN6AAAAAABMMA0GCSqG
# SIb3DQEBDAUAMGExCzAJBgNVBAYTAlVTMR4wHAYDVQQKExVNaWNyb3NvZnQgQ29y
# cG9yYXRpb24xMjAwBgNVBAMTKU1pY3Jvc29mdCBQdWJsaWMgUlNBIFRpbWVzdGFt
# cGluZyBDQSAyMDIwMB4XDTI0MTEyNjE4NDg1OVoXDTI1MTExOTE4NDg1OVowgdsx
# CzAJBgNVBAYTAlVTMRMwEQYDVQQIEwpXYXNoaW5ndG9uMRAwDgYDVQQHEwdSZWRt
# b25kMR4wHAYDVQQKExVNaWNyb3NvZnQgQ29ycG9yYXRpb24xJTAjBgNVBAsTHE1p
# Y3Jvc29mdCBBbWVyaWNhIE9wZXJhdGlvbnMxJzAlBgNVBAsTHm5TaGllbGQgVFNT
# IEVTTjo3ODAwLTA1RTAtRDk0NzE1MDMGA1UEAxMsTWljcm9zb2Z0IFB1YmxpYyBS
# U0EgVGltZSBTdGFtcGluZyBBdXRob3JpdHkwggIiMA0GCSqGSIb3DQEBAQUAA4IC
# DwAwggIKAoICAQDcde8XEX4HjETYu6YHtWiP7+6Vf2abeUo/si4NcaeiKrRMTF8F
# 7mpCoPJyo/h5VHbhyKDZazOm1cLuzKeVEMzDN4vuf3fZb5hSlpVlCXBSJ3YBLwLn
# RJtWNk+XkUMcAc96RdalToVYWltOIwbCCkjE42fnCafwjZajw1UGaxl4tRQNHwVk
# 5gwC2wlVVSJREJqCSsB9TXXHIKxPHnnFJqJ/LI1goJ+Ve0Bar4PiKiMfnvnZ8LR3
# ktW24X6FDQJRKLjnJQ0JVebQEvI+q8Y/frheUldXeLVD4SfQNl1fLKN58o+NJsWI
# 0ET6C8wYZc+eu+EqrzubIPXB7mKI9cbtmGHvztslz1K/NmRvGGQkeKEKdOWfpfRu
# YxmhmeVmR1QMLe5pBccJiXw7PUIW+3MB0pM5SBF5FH6INtT1gf5vHwBA9vbeiigg
# bijJMuK0qu63sIbbE/YN4iYrCURvjZampsTtxmlEtN921N0qXNtNgU0vavdc/vJl
# /rDef6fMeQuJAinIHxcJzPDTsOXZlegwcCr/J52eij6T9szMlPSCQVAt5u/agNcJ
# 212t6qdwZ4hYYF4LkCmXQgDPZpR1lGDCaojAB6zy/H7nME+nnTvTgTMtR4d4lHVB
# QxpJDnvYNvGPurrnP7FZT3ue8YzfFEiE5chmJia8THexs46F8tCr8T5UxQIDAQAB
# o4IByzCCAccwHQYDVR0OBBYEFGrqI3Sxu357rKTylpgwcVAF1Nw/MB8GA1UdIwQY
# MBaAFGtpKDo1L0hjQM972K9J6T7ZPdshMGwGA1UdHwRlMGMwYaBfoF2GW2h0dHA6
# Ly93d3cubWljcm9zb2Z0LmNvbS9wa2lvcHMvY3JsL01pY3Jvc29mdCUyMFB1Ymxp
# YyUyMFJTQSUyMFRpbWVzdGFtcGluZyUyMENBJTIwMjAyMC5jcmwweQYIKwYBBQUH
# AQEEbTBrMGkGCCsGAQUFBzAChl1odHRwOi8vd3d3Lm1pY3Jvc29mdC5jb20vcGtp
# b3BzL2NlcnRzL01pY3Jvc29mdCUyMFB1YmxpYyUyMFJTQSUyMFRpbWVzdGFtcGlu
# ZyUyMENBJTIwMjAyMC5jcnQwDAYDVR0TAQH/BAIwADAWBgNVHSUBAf8EDDAKBggr
# BgEFBQcDCDAOBgNVHQ8BAf8EBAMCB4AwZgYDVR0gBF8wXTBRBgwrBgEEAYI3TIN9
# AQEwQTA/BggrBgEFBQcCARYzaHR0cDovL3d3dy5taWNyb3NvZnQuY29tL3BraW9w
# cy9Eb2NzL1JlcG9zaXRvcnkuaHRtMAgGBmeBDAEEAjANBgkqhkiG9w0BAQwFAAOC
# AgEAAFYcd7rrNVHRZWofhE4ft9YNZPVEzaQ90iE/5kCDoQlCKTE7jFYnFcfxETrL
# 4ed8JSj0JxCZSJQVUwEp6haUSPkiSg4mf7rq+m3qbCjHB8Dj82rsFSxAs8NqI/08
# Dq1Ci/rxVhryPOSZmtXRgNeJzxwDqSch50pNBGQMU8APLSnwpqzhwRN76MK5PXYC
# Vqm/u/v579+fFJh0bIsw49/wTcTCXh3s0C9y0iAmSvsJKnTfEvtfe+eS9qw2wyf2
# LdJ5n8klFJ6OtDg8YB9n+E+0vX1EJIDPxN2yX7+2sJiABcUSc55jIHxPTArDdzR0
# YUwQIjZO0j9hIjyMbRYjgjJ4UK9ZLrvN2nUyc0upLqKKvhAqKP1jX0FL5M0wuneZ
# 9/SGy2ZFn/Bg8ISBOp34ri+412tOlzqR9ZU+CU9Xn1MqcWXvvDhTqjexxKZMVRMq
# GjRECQWSA62WdCGYjEOWnH5lQJqLYRhYpeAwvjszdEAjSFtFXFLGTRw4bSKoad5T
# jUEvsKFO8DVPCjrbMEzGdku4znmeFddbqXR41HlunpyOLuSoC1II/Bh+aX0nU19J
# U79T10OFRKZDFKUI3LWB9jTdT+3EOJr/pQ5T0fFeei0A7UdmTgXbmP4IaCbTc41N
# G7KMmsmV6Xyank4qB5aSL30uegrrvnHPjQBLLYjerGCNtQMxggdDMIIHPwIBATB4
# MGExCzAJBgNVBAYTAlVTMR4wHAYDVQQKExVNaWNyb3NvZnQgQ29ycG9yYXRpb24x
# MjAwBgNVBAMTKU1pY3Jvc29mdCBQdWJsaWMgUlNBIFRpbWVzdGFtcGluZyBDQSAy
# MDIwAhMzAAAATBtLnGPC5NN6AAAAAABMMA0GCWCGSAFlAwQCAQUAoIIEnDARBgsq
# hkiG9w0BCRACDzECBQAwGgYJKoZIhvcNAQkDMQ0GCyqGSIb3DQEJEAEEMBwGCSqG
# SIb3DQEJBTEPFw0yNTA5MTYwODA0MzVaMC8GCSqGSIb3DQEJBDEiBCAI5Xt1goSO
# Ww7RpSAglXJyQqzdSwn7YDBT3uirGtQELTCBuQYLKoZIhvcNAQkQAi8xgakwgaYw
# gaMwgaAEIN46bOoVmqp2Rt/G6TI8VIZkg7qJ8OddiPDqk6jY+midMHwwZaRjMGEx
# CzAJBgNVBAYTAlVTMR4wHAYDVQQKExVNaWNyb3NvZnQgQ29ycG9yYXRpb24xMjAw
# BgNVBAMTKU1pY3Jvc29mdCBQdWJsaWMgUlNBIFRpbWVzdGFtcGluZyBDQSAyMDIw
# AhMzAAAATBtLnGPC5NN6AAAAAABMMIIDXgYLKoZIhvcNAQkQAhIxggNNMIIDSaGC
# A0UwggNBMIICKQIBATCCAQmhgeGkgd4wgdsxCzAJBgNVBAYTAlVTMRMwEQYDVQQI
# EwpXYXNoaW5ndG9uMRAwDgYDVQQHEwdSZWRtb25kMR4wHAYDVQQKExVNaWNyb3Nv
# ZnQgQ29ycG9yYXRpb24xJTAjBgNVBAsTHE1pY3Jvc29mdCBBbWVyaWNhIE9wZXJh
# dGlvbnMxJzAlBgNVBAsTHm5TaGllbGQgVFNTIEVTTjo3ODAwLTA1RTAtRDk0NzE1
# MDMGA1UEAxMsTWljcm9zb2Z0IFB1YmxpYyBSU0EgVGltZSBTdGFtcGluZyBBdXRo
# b3JpdHmiIwoBATAHBgUrDgMCGgMVAJueWs/5vWNYP+JGxmOfpj88ZvzBoGcwZaRj
# MGExCzAJBgNVBAYTAlVTMR4wHAYDVQQKExVNaWNyb3NvZnQgQ29ycG9yYXRpb24x
# MjAwBgNVBAMTKU1pY3Jvc29mdCBQdWJsaWMgUlNBIFRpbWVzdGFtcGluZyBDQSAy
# MDIwMA0GCSqGSIb3DQEBCwUAAgUA7HMR1DAiGA8yMDI1MDkxNTIyMjk0MFoYDzIw
# MjUwOTE2MjIyOTQwWjB0MDoGCisGAQQBhFkKBAExLDAqMAoCBQDscxHUAgEAMAcC
# AQACAhwrMAcCAQACAhKPMAoCBQDsdGNUAgEAMDYGCisGAQQBhFkKBAIxKDAmMAwG
# CisGAQQBhFkKAwKgCjAIAgEAAgMHoSChCjAIAgEAAgMBhqAwDQYJKoZIhvcNAQEL
# BQADggEBAFZP5HZmbGfZjlJ8uGcYSixf4eMZ3sTcN5a12h6TEQfxw1TW0vFGcVAV
# Wc5x8DY3gohQd9WDMnMpSM7XQCLl6OMDC3grnxlwIrLm9qwn4+acrXdcW45wZ4jO
# StxvyPNEMklHtN20yFANvHTLK+1S4JRfQgAVpuIpihIPELyRwDHSMzYvFo4kTcou
# 4uIZItwfM6tcUadK+v2LdhmB2ZvQCcYe9fDS7gFocvRyHHfTn10JRkvCbDVfYq4V
# Ndiy0GtSSWipGugSOPA6neKYzi8nS2jS1q2Yp4/7y5aZbbZNXFjfUlP/xx/m2e2g
# BKPmQwQfUYMzeg/GHPlDY4fRjBPhk/IwDQYJKoZIhvcNAQEBBQAEggIAwHvvd9qE
# Ecx72C86vSa1EBBEwI2S8fmzZjgRJFsE/uHViM+Oc7QcLK6+vN7FKYPLYPfSByNU
# 7Q4Aep2tQlRg2zIqd6YvI2ERCZBml1cy4ZmHnrPRL17Bbpc6r5yY+1DOqtbVfSCM
# uxsJTGrqJibkYJzz6qKWcyw7uBrv41IHIMzxVxxvE9r3P6yaMxIvFJMUZKVTP0Gy
# fqwykMfgZ4K3H4PtC4fw9DESrRRDe4FRwHckQF5Hokcz1k0XMWV6G++7NI/Jxe7d
# W3FEvjyfPbNcgNJTUnREslH0qTd5ljNmn7L9XNv5yoxfjSvzUPVkG6sfCRXTZB1x
# 4Uh8gS8wMN6/BMxcErYFPTcHKC1v9xlQVAcfr6yIXeNLsVCVRNG/XvEgP+s341h6
# tZP9rB1NKno7gm6n40HwowZfKioAYRaCyln3giEJ21IpgdfO2N1KIhqAnrdLPpkq
# ZlJbA95czw43U01/iE2ufl2uckZjnRiBovvXj08gYsOcdgiYlG2kpaPyUK3JZR+5
# 8uFhemKN1BKppr0iKwwQfLEDzgK4C184ZZppEbM/LvKjMFxvY92bL5rbNX0+le4r
# g8RXdTDQ7ikwQCqfAOI/rASSgoNfSQKNuflCpnND9xJH6rnU9Pb7j2eBafU32f5Q
# 9k+ae9SIhaykz5ewBHgwgqI1zgeFfyrLou4=
# SIG # End signature block
