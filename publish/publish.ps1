# Script to build the WPF and CLI app, sign the executables and build the installer

Param (
    [switch]$dev,
    [switch]$preview,
    [ValidateSet("x64", "arm64")]
    [string[]]$arch = @("x64", "arm64")
)

if ($preview) {
    $WpfExeName = "2FAGuard-Preview.exe"
    $PortableExeName = "2FAGuard-Portable-Preview.exe"
    $CliExeName = "2fa-Preview.exe"

    $env:PREVIEW = "1"
}
else {
    $WpfExeName = "2FAGuard.exe"
    $PortableExeName = "2FAGuard-Portable.exe"
    $CliExeName = "2fa.exe"
}

function Confirm-Requirements {
    # Check if current directory is the publish directory
    if (-not (Test-Path .\publish.ps1)) {
        Write-Host "Please run the script from the publish directory"
        Exit
    }
    # Check if Inno Setup 7 is installed
    if (-not (Test-Path "C:\Program Files\Inno Setup 7\ISCC.exe")) {
        Write-Host "Inno Setup 7 is required to build the installer"
        Exit
    }
    # Check if the command sign tool is available
    if (-not (Get-Command "signtool.exe" -errorAction SilentlyContinue)) {
        Write-Host "SignTool is required to sign the executables"
        Exit
    }
    # Check if the command dotnet is available
    if (-not (Get-Command "dotnet" -errorAction SilentlyContinue)) {
        Write-Host "Dotnet is required to build the executables"
        Exit
    }
    # Create required directories
    if (-not (Test-Path .\bin)) {
        New-Item -ItemType Directory -Path .\bin
    }
}

function Assert-Success($step, $exitCode) {
    if ($exitCode -ne 0) {
        Write-Host "$step failed (exit code $exitCode)" -ForegroundColor Red
        Exit 1
    }
}

function Invoke-Tests {
    Set-Location ../Guard.Test
    dotnet test
    Assert-Success "Tests" $LASTEXITCODE
    Set-Location ../publish
}

function Build-WPF-App($a) {
    $outDir = "..\publish\bin\$a"
    New-Item -ItemType Directory -Path $outDir -Force | Out-Null
    Set-Location ../Guard.WPF
    dotnet publish -r "win-$a" -c Release --p:PublishSingleFile=true --self-contained true -o "bin\publish-$a"
    Assert-Success "WPF publish ($a)" $LASTEXITCODE
    Move-Item "bin\publish-$a\2FAGuard.exe" "$outDir\$WpfExeName" -Force
    dotnet publish -r "win-$a" -c Release --p:PublishSingleFile=true --self-contained true -p:IsPortable=true -o "bin\portable-$a"
    Assert-Success "WPF portable publish ($a)" $LASTEXITCODE
    Move-Item "bin\portable-$a\2FAGuard.exe" "$outDir\$PortableExeName" -Force
    Set-Location ../publish
}

function Build-CLI-App($a) {
    $outDir = "..\publish\bin\$a"
    New-Item -ItemType Directory -Path $outDir -Force | Out-Null
    Set-Location ../Guard.CLI
    dotnet publish -r "win-$a" -c Release --p:PublishSingleFile=true --self-contained true -o "bin\publish-$a"
    Assert-Success "CLI publish ($a)" $LASTEXITCODE
    Move-Item "bin\publish-$a\2fa.exe" "$outDir\$CliExeName" -Force
    Set-Location ../publish
}

function Invoke-Code-Signing {
    $files = foreach ($a in $arch) {
        ".\bin\$a\$WpfExeName"
        ".\bin\$a\$PortableExeName"
        ".\bin\$a\$CliExeName"
    }
    signtool.exe sign /sha1 43E9DD4D4A06853CBB521EA35E5F337EDB0DBCCD /t "http://time.certum.pl/" /fd sha256 /d "2FAGuard" /du "https://2faguard.app" $files
    Assert-Success "Code signing" $LASTEXITCODE
}

function Build-Installer($a) {
    $p = Start-Process -NoNewWindow -Wait -PassThru -FilePath "C:\Program Files\Inno Setup 7\ISCC.exe" -ArgumentList "/DArch=$a", "./installer.iss"
    Assert-Success "Installer build ($a)" $p.ExitCode
}


Write-Host "Checking Requirements"
Confirm-Requirements

Write-Host "Running Tests"
Invoke-Tests

foreach ($a in $arch) {
    Write-Host "Building WPF App ($a)"
    Build-WPF-App $a

    Write-Host "Building CLI App ($a)"
    Build-CLI-App $a
}

if ($dev) {
    Write-Host "Dev mode enabled, skipping signing and installer"
    Exit 0
}

Write-Host "Signing Code"
Invoke-Code-Signing

foreach ($a in $arch) {
    Write-Host "Building Installer ($a)"
    Build-Installer $a
}
Write-Host "Done"

# SIG # Begin signature block
# MIIoeQYJKoZIhvcNAQcCoIIoajCCKGYCAQExDzANBglghkgBZQMEAgEFADB5Bgor
# BgEEAYI3AgEEoGswaTA0BgorBgEEAYI3AgEeMCYCAwEAAAQQH8w7YFlLCE63JNLG
# KX7zUQIBAAIBAAIBAAIBAAIBADAxMA0GCWCGSAFlAwQCAQUABCDt+50RwTsANy/l
# 8DTPelzshc3RRACLfapkwOGEjJJCrKCCILYwggXJMIIEsaADAgECAhAbtY8lKt8j
# AEkoya49fu0nMA0GCSqGSIb3DQEBDAUAMH4xCzAJBgNVBAYTAlBMMSIwIAYDVQQK
# ExlVbml6ZXRvIFRlY2hub2xvZ2llcyBTLkEuMScwJQYDVQQLEx5DZXJ0dW0gQ2Vy
# dGlmaWNhdGlvbiBBdXRob3JpdHkxIjAgBgNVBAMTGUNlcnR1bSBUcnVzdGVkIE5l
# dHdvcmsgQ0EwHhcNMjEwNTMxMDY0MzA2WhcNMjkwOTE3MDY0MzA2WjCBgDELMAkG
# A1UEBhMCUEwxIjAgBgNVBAoTGVVuaXpldG8gVGVjaG5vbG9naWVzIFMuQS4xJzAl
# BgNVBAsTHkNlcnR1bSBDZXJ0aWZpY2F0aW9uIEF1dGhvcml0eTEkMCIGA1UEAxMb
# Q2VydHVtIFRydXN0ZWQgTmV0d29yayBDQSAyMIICIjANBgkqhkiG9w0BAQEFAAOC
# Ag8AMIICCgKCAgEAvfl4+ObVgAxknYYblmRnPyI6HnUBfe/7XGeMycxca6mR5rlC
# 5SBLm9qbe7mZXdmbgEvXhEArJ9PoujC7Pgkap0mV7ytAJMKXx6fumyXvqAoAl4Va
# qp3cKcniNQfrcE1K1sGzVrihQTib0fsxf4/gX+GxPw+OFklg1waNGPmqJhCrKtPQ
# 0WeNG0a+RzDVLnLRxWPa52N5RH5LYySJhi40PylMUosqp8DikSiJucBb+R3Z5yet
# /5oCl8HGUJKbAiy9qbk0WQq/hEr/3/6zn+vZnuCYI+yma3cWKtvMrTscpIfcRnNe
# GWJoRVfkkIJCu0LW8GHgwaM9ZqNd9BjuiMmNF0UpmTJ1AjHuKSbIawLmtWJFfzcV
# WiNoidQ+3k4nsPBADLxNF8tNorMe0AZa3faTz1d1mfX6hhpneLO/lv403L3nUlbl
# s+V1e9dBkQXcXWnjlQ1DufyDljmVe2yAWk8TcsbXfSl6RLpSpCrVQUYJIP4ioLZb
# MI28iQzV13D4h1L92u+sUS4Hs07+0AnacO+Y+lbmbdu1V0vc5SwlFcieLnhO+Nqc
# noYsylfzGuXIkosagpZ6w7xQEmnYDlpGizrrJvojybawgb5CAKT41v4wLsfSRvbl
# jnX98sy50IdbzAYQYLuDNbdeZ95H7JlI8aShFf6tjGKOOVVPORa5sWOd/7cCAwEA
# AaOCAT4wggE6MA8GA1UdEwEB/wQFMAMBAf8wHQYDVR0OBBYEFLahVDkCw6A/joq8
# +tT4HKbROg79MB8GA1UdIwQYMBaAFAh2zcsH/yT2xc3tu5C84oQ3RnX3MA4GA1Ud
# DwEB/wQEAwIBBjAvBgNVHR8EKDAmMCSgIqAghh5odHRwOi8vY3JsLmNlcnR1bS5w
# bC9jdG5jYS5jcmwwawYIKwYBBQUHAQEEXzBdMCgGCCsGAQUFBzABhhxodHRwOi8v
# c3ViY2Eub2NzcC1jZXJ0dW0uY29tMDEGCCsGAQUFBzAChiVodHRwOi8vcmVwb3Np
# dG9yeS5jZXJ0dW0ucGwvY3RuY2EuY2VyMDkGA1UdIAQyMDAwLgYEVR0gADAmMCQG
# CCsGAQUFBwIBFhhodHRwOi8vd3d3LmNlcnR1bS5wbC9DUFMwDQYJKoZIhvcNAQEM
# BQADggEBAFHCoVgWIhCL/IYx1MIy01z4S6Ivaj5N+KsIHu3V6PrnCA3st8YeDrJ1
# BXqxC/rXdGoABh+kzqrya33YEcARCNQOTWHFOqj6seHjmOriY/1B9ZN9DbxdkjuR
# mmW60F9MvkyNaAMQFtXx0ASKhTP5N+dbLiZpQjy6zbzUeulNndrnQ/tjUoCFBMQl
# lVXwfqefAcVbKPjgzoZwpic7Ofs4LphTZSJ1Ldf23SIikZbr3WjtP6MZl9M7JYjs
# NhI9qX7OAo0FmpKnJ25FspxihjcNpDOO16hO0EoXQ0zF8ads0h5YbBRRfopUofbv
# n3l6XYGaFpAP4bvxSgD5+d2+7arszgowggaCMIIEaqADAgECAhAo8HfBHDa9/l90
# MkdwJy4DMA0GCSqGSIb3DQEBDAUAMFYxCzAJBgNVBAYTAlBMMSEwHwYDVQQKExhB
# c3NlY28gRGF0YSBTeXN0ZW1zIFMuQS4xJDAiBgNVBAMTG0NlcnR1bSBUaW1lc3Rh
# bXBpbmcgMjAyMSBDQTAeFw0yNjAzMTEwNzM0NTRaFw0zNjAyMjcwNzM0NTRaMFAx
# CzAJBgNVBAYTAlBMMSEwHwYDVQQKDBhBc3NlY28gRGF0YSBTeXN0ZW1zIFMuQS4x
# HjAcBgNVBAMMFUNlcnR1bSBUaW1lc3RhbXAgMjAyNjCCAiIwDQYJKoZIhvcNAQEB
# BQADggIPADCCAgoCggIBALi9zfBw+xI12hi2F9uNQBNOSTkTIi13fuh3DiWybI53
# 92bBfJw6tO7zDs7UqP/rejVf4xFTU3XNOPsuOFkP5SPFsnWwn3VJL+NhNhsuujvY
# sQtaQnvNo1wk46reTgws1L8OFEdKaxgyqT4zOQx81mLwNZSqXNU9Rrb9oAJWJF4M
# ydz+C1ebu0D0+vgM3tqAQ8TU203fqgKCPqIdHloaFqH9eyR+8tnpmy2hnnN59ZOt
# msuHeRwf5iTOhlhf0d/qqquLOSIGPgGVL1Hj5XBUox2OE7tQRr+l/xxNbL6OuwBO
# 4Aa2EbuxYg37cpXv5i2/lTL4NASB/duf9W4fjA3Ro+GP+bC54n2OYiM3Cq8z92ej
# x8F5HWO2e4/75eC95B9tMw/eq7109GDyW2+2ut5Bn0o8MXJTERkyystWbqcnUkom
# A97qfiDCwvpWuOMdfTOksXe9S+xDxOJaCbiyTivgPulxcT+tfQTRXBr5UwtPvc+I
# NnafOEilzVHA11tv+pHcylryxBc08cUihf/ttqkZS0bmx3VxQay8uohaTp0//ECA
# 9vYGW51SKdJMJhXvKyIdBLy8WYNchTt8yRITv9Adxg9JqGujh+TnP1bW3aQjFtlE
# l7SzgNeI8OVA/CJAWWUuRT3Au9ToOpHy/flrrknhN4heEw7/6ILMv4HSsj0YZ4ot
# AgMBAAGjggFQMIIBTDB1BggrBgEFBQcBAQRpMGcwOwYIKwYBBQUHMAKGL2h0dHA6
# Ly9zdWJjYS5yZXBvc2l0b3J5LmNlcnR1bS5wbC9jdHNjYTIwMjEuY2VyMCgGCCsG
# AQUFBzABhhxodHRwOi8vc3ViY2Eub2NzcC1jZXJ0dW0uY29tMB8GA1UdIwQYMBaA
# FL5UAi+/QGxzQ86sCSVOnkNEGu7gMAwGA1UdEwEB/wQCMAAwOQYDVR0fBDIwMDAu
# oCygKoYoaHR0cDovL3N1YmNhLmNybC5jZXJ0dW0ucGwvY3RzY2EyMDIxLmNybDAW
# BgNVHSUBAf8EDDAKBggrBgEFBQcDCDAOBgNVHQ8BAf8EBAMCB4AwIgYDVR0gBBsw
# GTAIBgZngQwBBAIwDQYLKoRoAYb2dwIFAQswHQYDVR0OBBYEFCM5aiirmhKnpKfy
# H/lFmr+N/oIDMA0GCSqGSIb3DQEBDAUAA4ICAQBm/pObrAveTqYFPCSthxNVwBHT
# W4TfuBQY2KGCdMlH6ALjVPYf2XARNGMTXScR0JQ2/c2LxLpoHHhRwxFf3d933DhF
# S165rYj6iPSQKm41rc9AMpTCjMdmai1aK9o870pOzTzLRhWFVIxm+5Qf+t4V5z14
# tPoUsAWdfRKD1Zzvzfe5AHlE9rxOZcGROiULxs3Edf7ZJ8RZI0jzD9juj2oEX2XS
# 7BVKuNCK1W0h1mjapv028yqo8UtZh5/1Ib48Yyp88BDYShh5PSGUUuS5tYXVfcdu
# IPBe3PdNTYRYdT5VNxkwLgjMzIcr/6SH2fejFJDjnCxjvXLuarErJg/IdI+hXdGI
# BDVOGv1zrkF4ktoQvFZIp5DIAYh0GwyUhc87qqn5UaYTj2+iyiBDSNh7aGCrkuAn
# qg7Qh7frhX+QhMGeJ9SpLpnG76qN85hLyjvYR6gRYiLTj8G4fxjqV400/buPw0rB
# o9yJZyq/8cGdCOLPOYf9qpefTcrZKeO9pg+UJHx8UABbaTqizRxm8sUSEycccCCF
# hIit3AI/3wGuSOADeiA676NcQ7+dsP04YvzvdN7pTZNyg672WpBDJdg0eItHl4yo
# mvNeIOWh1qs4QqOW6TI8Ocsi4uSDTLorxLUDnBTwKQzzpxd1YUctEgRh32R0J9Fk
# FkeEruJU94RljFr1uDCCBrkwggShoAMCAQICEQCZo4AKJlU7ZavcboSms+o5MA0G
# CSqGSIb3DQEBDAUAMIGAMQswCQYDVQQGEwJQTDEiMCAGA1UEChMZVW5pemV0byBU
# ZWNobm9sb2dpZXMgUy5BLjEnMCUGA1UECxMeQ2VydHVtIENlcnRpZmljYXRpb24g
# QXV0aG9yaXR5MSQwIgYDVQQDExtDZXJ0dW0gVHJ1c3RlZCBOZXR3b3JrIENBIDIw
# HhcNMjEwNTE5MDUzMjE4WhcNMzYwNTE4MDUzMjE4WjBWMQswCQYDVQQGEwJQTDEh
# MB8GA1UEChMYQXNzZWNvIERhdGEgU3lzdGVtcyBTLkEuMSQwIgYDVQQDExtDZXJ0
# dW0gQ29kZSBTaWduaW5nIDIwMjEgQ0EwggIiMA0GCSqGSIb3DQEBAQUAA4ICDwAw
# ggIKAoICAQCdI88EMCM7wUYs5zNzPmNdenW6vlxNur3rLfi+5OZ+U3iZIB+AspO+
# CC/bj+taJUbMbFP1gQBJUzDUCPx7BNLgid1TyztVLn52NKgxxu8gpyTr6EjWyGzK
# U/gnIu+bHAse1LCitX3CaOE13rbuHbtrxF2tPU8f253QgX6eO8yTbGps1Mg+yda3
# DcTsOYOhSYNCJiL+5wnjZ9weoGRtvFgMHtJg6i671OPXIciiHO4Lwo2p9xh/tnj+
# JmCQEn5QU0NxzrOiRna4kjFaA9ZcwSaG7WAxeC/xoZSxF1oK1UPZtKVt+yrsGKqW
# ONoK6f5EmBOAVEK2y4ATDSkb34UD7JA32f+Rm0wsr5ajzftDhA5mBipVZDjHpwzv
# 8bTKzCDUSUuUmPo1govD0RwFcTtMXcfJtm1i+P2UNXadPyYVKRxKQATHN3imsfBi
# NRdN5kiVVeqP55piqgxOkyt+HkwIA4gbmSc3hD8ke66t9MjlcNg73rZZlrLHsAIV
# /nJ0mmgSjBI/TthoGJDydekOQ2tQD2Dup/+sKQptalDlui59SerVSJg8gAeV7N/i
# a4mrGoiez+SqV3olVfxyLFt3o/OQOnBmjhKUANoKLYlKmUpKEFI0PfoT8Q1W/y6s
# 9LTI6ekbi0igEbFUIBE8KDUGfIwnisEkBw5KcBZ3XwnHmfznwlKo8QIDAQABo4IB
# VTCCAVEwDwYDVR0TAQH/BAUwAwEB/zAdBgNVHQ4EFgQU3XRdTADbe5+gdMqxbvc8
# wDLAcM0wHwYDVR0jBBgwFoAUtqFUOQLDoD+Oirz61PgcptE6Dv0wDgYDVR0PAQH/
# BAQDAgEGMBMGA1UdJQQMMAoGCCsGAQUFBwMDMDAGA1UdHwQpMCcwJaAjoCGGH2h0
# dHA6Ly9jcmwuY2VydHVtLnBsL2N0bmNhMi5jcmwwbAYIKwYBBQUHAQEEYDBeMCgG
# CCsGAQUFBzABhhxodHRwOi8vc3ViY2Eub2NzcC1jZXJ0dW0uY29tMDIGCCsGAQUF
# BzAChiZodHRwOi8vcmVwb3NpdG9yeS5jZXJ0dW0ucGwvY3RuY2EyLmNlcjA5BgNV
# HSAEMjAwMC4GBFUdIAAwJjAkBggrBgEFBQcCARYYaHR0cDovL3d3dy5jZXJ0dW0u
# cGwvQ1BTMA0GCSqGSIb3DQEBDAUAA4ICAQB1iFgP5Y9QKJpTnxDsQ/z0O23JmoZi
# fZdEOEmQvo/79PQg9nLF/GJe6ZiUBEyDBHMtFRK0mXj3Qv3gL0sYXe+PPMfwmreJ
# HvgFGWQ7XwnfMh2YIpBrkvJnjwh8gIlNlUl4KENTK5DLqsYPEtRQCw7R6p4s2EtW
# yDDr/M58iY2UBEqfUU/ujR9NuPyKk0bEcEi62JGxauFYzZ/yld13fHaZskIoq2Xa
# zjaD0pQkcQiIueL0HKiohS6XgZuUtCKA7S6CHttZEsObQJ1j2s0urIDdqF7xaXFV
# aTHKtAuMfwi0jXtF3JJphrJfc+FFILgCbX/uYBPBlbBIP4Ht4xxk2GmfzMn7oxPI
# TpigQFJFWuzTMUUgdRHTxaTSKRJ/6Uh7ki/pFjf9sUASWgxT69QF9Ki4JF5nBIuj
# xZ2sOU9e1HSCJwOfK07t5nnzbs1LbHuAIGJsRJiQ6HX/DW1XFOlXY1rc9HufFhWU
# +7Uk+hFkJsfzqBz3pRO+5aI6u5abI4Qws4YaeJH7H7M8X/YNoaArZbV4Ql+jarKs
# E0+8XvC4DJB+IVcvC9Ydqahi09mjQse4fxfef0L7E3hho2O3bLDM6v60rIRUCi2f
# JT2/IRU5ohgyTch4GuYWefSBsp5NPJh4QRTP9DC3gc5QEKtbrTY0Ka87Web7/zSc
# vLmvQBm8JDFpDjCCBrkwggShoAMCAQICEQDn/2nHOzXOS5Em2HR8aKWHMA0GCSqG
# SIb3DQEBDAUAMIGAMQswCQYDVQQGEwJQTDEiMCAGA1UEChMZVW5pemV0byBUZWNo
# bm9sb2dpZXMgUy5BLjEnMCUGA1UECxMeQ2VydHVtIENlcnRpZmljYXRpb24gQXV0
# aG9yaXR5MSQwIgYDVQQDExtDZXJ0dW0gVHJ1c3RlZCBOZXR3b3JrIENBIDIwHhcN
# MjEwNTE5MDUzMjA3WhcNMzYwNTE4MDUzMjA3WjBWMQswCQYDVQQGEwJQTDEhMB8G
# A1UEChMYQXNzZWNvIERhdGEgU3lzdGVtcyBTLkEuMSQwIgYDVQQDExtDZXJ0dW0g
# VGltZXN0YW1waW5nIDIwMjEgQ0EwggIiMA0GCSqGSIb3DQEBAQUAA4ICDwAwggIK
# AoICAQDpEh8ENe25XXrFppVBvoplf0530W0lddNmjtv4YSh/f7eDQKFaIqc7tHj7
# ox+u8vIsJZlroakUeMS3i3T8aJRC+eQs4FF0GqvkM6+WZO8kmzZfxmZaBYmMLs8F
# ktgFYCzywmXeQ1fEExflee2OpbHVk665eXRHjH7MYZIzNnjl2m8Hy8ulB9mR8wL/
# W0v0pjKNT6G0sfrx1kk+3OGosFUb7yWNnVkWKU4qSxLv16kJ6oVJ4BSbZ4xMak6J
# LeB8szrK9vwGDpvGDnKCUMYL3NuviwH1x4gZG0JAXU3x2pOAz91JWKJSAmRy/l0s
# 0l5bEYKolg+DMqVhlOANd8Yh5mkQWaMEvBRE/kAGzIqgWhwzN2OsKIVtO8mf5sPW
# SrvyplSABAYa13rMYnzwfg08nljZHghquCJYCa/xHK9acev9UD7Y+usr15d7mrsz
# zxhF1JOr1Mpup2chNSBlyOObhlSO16rwrffVrg/SzaKfSndS5swRhr8bnDqNJY9T
# NyEYvBYpgF95K7p0g4LguR4A++Z1nFIHWVY5v0fNVZmgzxD9uVo/gta3onGOQj3J
# CxgYx0KrCXu4yc9QiVwTFLWbNdHFSjBCt5/8Q9pLuRhVocdCunhcHudMS1CGQ/Rn
# 0+7P+fzMgWdRKfEOh/hjLrnQ8BdJiYrZNxvIOhM2aa3zEDHNwwIDAQABo4IBVTCC
# AVEwDwYDVR0TAQH/BAUwAwEB/zAdBgNVHQ4EFgQUvlQCL79AbHNDzqwJJU6eQ0Qa
# 7uAwHwYDVR0jBBgwFoAUtqFUOQLDoD+Oirz61PgcptE6Dv0wDgYDVR0PAQH/BAQD
# AgEGMBMGA1UdJQQMMAoGCCsGAQUFBwMIMDAGA1UdHwQpMCcwJaAjoCGGH2h0dHA6
# Ly9jcmwuY2VydHVtLnBsL2N0bmNhMi5jcmwwbAYIKwYBBQUHAQEEYDBeMCgGCCsG
# AQUFBzABhhxodHRwOi8vc3ViY2Eub2NzcC1jZXJ0dW0uY29tMDIGCCsGAQUFBzAC
# hiZodHRwOi8vcmVwb3NpdG9yeS5jZXJ0dW0ucGwvY3RuY2EyLmNlcjA5BgNVHSAE
# MjAwMC4GBFUdIAAwJjAkBggrBgEFBQcCARYYaHR0cDovL3d3dy5jZXJ0dW0ucGwv
# Q1BTMA0GCSqGSIb3DQEBDAUAA4ICAQC4k1l3yUwV/ZQHCKCneqAs8EGTnwEUJLdD
# pokN/dMhKjK0rR5qX8nIIHzxpQR3TAw2IRw1Uxsr2PliG3bCFqSdQTUbfaTq6V3v
# BzEebDru9QFjqlKnxCF2h1jhLNFFplbPJiW+JSnJTh1fKEqEdKdxgl9rVTvlxfEJ
# 7exOn25MGbd/wGPwuSmMxRJVO0wnqgS7kmoJjNF9zqeehFSDDP8ZVkWg4EZ2tIS0
# M3uZmByRr+1Lkwjjt8AtW83mVnZTyTsOb+FNfwJY7DS4FmWhkRbgcHRetreoTirP
# Or/ozyDKhT8MTSTf6Lttg6s6T/u08mDWw6HK04ZRDfQ9sb77QV8mKgO44WGP31vX
# nVKoWVJpFBjPvjL8/Zck/5wXX2iqjOaLStFOR/IQki+Ehn4zlcgVm22ZVCBPF+l8
# nAwUUShCtKuSU7GmZLKCmmxQMkSiWILTm8EtVD6AxnJhoq8EnhjEEyUoflkeRF2W
# hFiVQOmWTwZRr44IxWGkNJC6tTorW5rl2Zl+2e9JLPYf3pStAPMDoPKIjVXd6NW2
# +fZrNUBeDo2eOa5Fn7Brs/HLQff5Xgris5MeUbdVgDrF8uxO6cLPvZPo63j62SsN
# g55pTWk9fUIF9iPoRbb4QurjoY/woI1RAOKtYtTic6aAJq3u83RIPpGXBSJKwx4K
# JAOZnCDCtTCCBuUwggTNoAMCAQICEEMiutwJdzYa7wf8NlVHjZQwDQYJKoZIhvcN
# AQELBQAwVjELMAkGA1UEBhMCUEwxITAfBgNVBAoTGEFzc2VjbyBEYXRhIFN5c3Rl
# bXMgUy5BLjEkMCIGA1UEAxMbQ2VydHVtIENvZGUgU2lnbmluZyAyMDIxIENBMB4X
# DTI2MDIwMTEwMDIwMloXDTI3MDIwMTEwMDIwMVowgYoxCzAJBgNVBAYTAkRFMRww
# GgYDVQQIDBNOb3JkcmhlaW4tV2VzdGZhbGVuMQ4wDAYDVQQHDAVXZXNlbDEeMBwG
# A1UECgwVT3BlbiBTb3VyY2UgRGV2ZWxvcGVyMS0wKwYDVQQDDCRPcGVuIFNvdXJj
# ZSBEZXZlbG9wZXIsIFRpbW8gS8O2c3NsZXIwggIiMA0GCSqGSIb3DQEBAQUAA4IC
# DwAwggIKAoICAQDXCm+iTaHTq8vqlkNFaqLu0TWBkcrOlD4epgynNcaUBmq8ey4E
# e+A2+Dt4fMuM8cQDyPGq/KG3lReNpU8RTbHEh1YZYtLhLFViMDvmh2SP9giuQDap
# dQiuLquVn39z0dlUfF+aT2yX0tnZRWcvkn9ha3HYlHXmiXG+ZJBmoH31E/If6xDW
# 1J5jZT4S8SahFHjGYS8atyZqk8jsuze/Wy5tcqMfkK50hvz39by7xi0PgtJ+tyus
# SUFv8xR4Q9UOivd6DaPEmXiC/78C1sP05i/hEV5OclWOd2ekd9GJoJSpL0FHviVR
# f391jLZbDozTY9uNhlZmoU6wmCit/17m7T6gY2AP30WCUftOxjbrx+RyuyrT1cDc
# sMc5ToD1Ih3mye+9BDxHyLMsauGfjYSPRZvNPGJjzO5foMeZzVa9OaYnfmYTz2WA
# SeffArSD51KID2yipj+bGYUpGGuaKQXJDg27HyoAellFL/5Ode50flpmO0dssU7L
# TBTuWUmPc7H00HQGftRdUzUD3b5s0IWBpN5GWz/P37l7/nOc79ZhgVDLT9ZNXxzv
# DLMu4VlF+MVWPaKH+NMsXlYrwktHoEFWp+L5JEjrdBO64hUyh2nJKXOtw2hA0dIP
# Rcn9LZu7Ig1Eta6kuYDiClIcflPhtBddF9ZoL4fCRIk3eAcXMwpz/AQ8IQIDAQAB
# o4IBeDCCAXQwDAYDVR0TAQH/BAIwADA9BgNVHR8ENjA0MDKgMKAuhixodHRwOi8v
# Y2NzY2EyMDIxLmNybC5jZXJ0dW0ucGwvY2NzY2EyMDIxLmNybDBzBggrBgEFBQcB
# AQRnMGUwLAYIKwYBBQUHMAGGIGh0dHA6Ly9jY3NjYTIwMjEub2NzcC1jZXJ0dW0u
# Y29tMDUGCCsGAQUFBzAChilodHRwOi8vcmVwb3NpdG9yeS5jZXJ0dW0ucGwvY2Nz
# Y2EyMDIxLmNlcjAfBgNVHSMEGDAWgBTddF1MANt7n6B0yrFu9zzAMsBwzTAdBgNV
# HQ4EFgQUa9zFNOrTd6M5n/+97kKncd92ve8wSwYDVR0gBEQwQjAIBgZngQwBBAEw
# NgYLKoRoAYb2dwIFAQQwJzAlBggrBgEFBQcCARYZaHR0cHM6Ly93d3cuY2VydHVt
# LnBsL0NQUzATBgNVHSUEDDAKBggrBgEFBQcDAzAOBgNVHQ8BAf8EBAMCB4AwDQYJ
# KoZIhvcNAQELBQADggIBACO9tg1yqrj+81Irn0PvSI7xRwl2d1a2BKNlRrwXR4Rs
# crFSX4P2FAJ1m5CZR3sr9cWDBHOyfQRzujB/2gxrcxydnc9W8WrM94arbqcR2Apy
# TWqxQ4CxVfJ0fajleBc1tu3Dn5A7rIoJUjnwcM+u3ZkpIUAfrAoplEk8SffIu89e
# Xoe5LMyjIrjJTUH7XdygtR9g/cuG3WGFvB/r6AMSpdG12HVXw6njkfIxGviZV9yZ
# Y1IoVs2HCKz/CyMp9qYjY423A0kR/ordCVHFVfIv8remWD2ctkKDfHAhooB9Lmbb
# vDPcZh+J1zEeqANe252fg0Uk3URZoA9V/lKWREkkSODXbQ9TjRqf7F0ahgN8h8n5
# VAaeTEt1NdvSCY0VoJVA9JAT8bEe4WqyeK7F2zOw1vltC/69RcUFOaEhcEQoS4WP
# +YKfncmXyDSMioZdPRbinGRGsJ5UIv68xImxvsg/nXnYuCldOSjuU/eqDPpY6w/c
# axadq1LAoM1ZTT6w4Of3YjFqg7vbqsDvwuTY7H/eBsINoLqKPgpHmc2DcGQZwT+U
# Y/wy1l7+GPwtZZhsoXLeOUAcRju4YXUqUUly46PejIZgd2Je+FGjePkDCOJeYdlq
# NOgE4XJNl/pbQtw2QEHqz99OVDB6sJFJdUxWz7TEASL5+deNHD2X9d6T6yWMbtDm
# MYIHGTCCBxUCAQEwajBWMQswCQYDVQQGEwJQTDEhMB8GA1UEChMYQXNzZWNvIERh
# dGEgU3lzdGVtcyBTLkEuMSQwIgYDVQQDExtDZXJ0dW0gQ29kZSBTaWduaW5nIDIw
# MjEgQ0ECEEMiutwJdzYa7wf8NlVHjZQwDQYJYIZIAWUDBAIBBQCgfDAQBgorBgEE
# AYI3AgEMMQIwADAZBgkqhkiG9w0BCQMxDAYKKwYBBAGCNwIBBDAcBgorBgEEAYI3
# AgELMQ4wDAYKKwYBBAGCNwIBFTAvBgkqhkiG9w0BCQQxIgQgfjA3Kcgo98hmQdEI
# rHiSSFcJNTF90uOANKyjTTXZmgwwDQYJKoZIhvcNAQEBBQAEggIAg8FhFeLva6BG
# j1AumexLs5KccZnadfSvdFztr/ouLBr6fzPuAEZKjs5DzgKIqP8bMnmYXl7+Dccn
# syyqrtM3JAIdSWZZyA1GKMEK0YIkn84G4j1ceHmZcVnV+iy8cAw09zSMYr+XjmRO
# t47uDpiU48UNL6yLyGx+oUgy2xcsvQFvmA/NWmPKoGsg3/sVTyqjD8kVg2bhvoea
# ZY5ePA3zE9nk3a2VtvZILpFo4j8yQ6CkbMeaIT6Cs1lk6+NnyxdDQnr9zHpfGLnY
# G6X7rqQ5YsfKWbyr/uUbBIoqCNuya7PUxQgB2pStMeQX0xeZBfvszu4TJI4QD//j
# s1O2NUfiN3S8mFyVrTQQhtgb4HMPsa3jmS3vVNTCcqS598mjKBLSjD7AwDJSO44y
# 92ym8Orb3oGX2gwJoRjUQqh5vDzzBJhnjDH6gliEReJcsX1Gu70zntaSW9vsNfKq
# c5YpHae8gEkrqh4nLkQTeu+9wU3mg6MWAEspodhcXqiNIV4eFuB6f12QKvGch2Lg
# UzpBmDICAKyxOf2x3f6rXZukurdtF7URdQc8H55u0qEcMGkgMnVJHE9aInSQIN2m
# i4dKto8Pvb5w4CqkZ7xXyKy9K8OQdgMKAFQ43ZIugNx+COFHWhrZmjWVB8vZ2uOi
# zhPxkO9yfh+CECdKkvQYPmrAzzcQSbmhggQCMIID/gYJKoZIhvcNAQkGMYID7zCC
# A+sCAQEwajBWMQswCQYDVQQGEwJQTDEhMB8GA1UEChMYQXNzZWNvIERhdGEgU3lz
# dGVtcyBTLkEuMSQwIgYDVQQDExtDZXJ0dW0gVGltZXN0YW1waW5nIDIwMjEgQ0EC
# ECjwd8EcNr3+X3QyR3AnLgMwDQYJYIZIAWUDBAICBQCgggFWMBoGCSqGSIb3DQEJ
# AzENBgsqhkiG9w0BCRABBDAcBgkqhkiG9w0BCQUxDxcNMjYxMTAyMTgyNzMzWjA3
# BgsqhkiG9w0BCRACLzEoMCYwJDAiBCCFvpDhCtJDjXzJKLavSLCasggXfOz4sBJs
# WNORBSXEPDA/BgkqhkiG9w0BCQQxMgQwT9golyfKqSlspSURXymOEJi88NxyoWnf
# 2jlOaP187xqnFweoJkL5s+k2KP9kK0yAMIGfBgsqhkiG9w0BCRACDDGBjzCBjDCB
# iTCBhgQUVxRoQQyoWvNCTvkWSlE2EPTTjZgwbjBapFgwVjELMAkGA1UEBhMCUEwx
# ITAfBgNVBAoTGEFzc2VjbyBEYXRhIFN5c3RlbXMgUy5BLjEkMCIGA1UEAxMbQ2Vy
# dHVtIFRpbWVzdGFtcGluZyAyMDIxIENBAhAo8HfBHDa9/l90MkdwJy4DMA0GCSqG
# SIb3DQEBAQUABIICALVCXt5fIkx5a0vFbPRJq1IbHpSF1OM+nfcTdZze9FwONN/P
# n26NJu1SPbZfKoK8zDOrnR6lkFZm4UUydCFN1yHa4YH19I42gZLa0fqLKvxwi1q9
# FgoazqODzRMnpY4ElSfpKuhQm1PU5pJDy1gNCwsdf8WJcyi/e/DnqpRGOiCE1MBz
# 5VmvTz/YPoi977pejz10X/jfh2KFpBTFsFX748bXa1Fbn1IhJGOxWj0t8jQ+BuRQ
# gRbdAPFkhRcaKw+QoCEqd3mg1ztNFkbYe2rUpICmZ4zZs0QTB6dloPPCyn+PHBAE
# kHRS80T+oY4c18mAT9GzwCuCc6ezya8VrCZyDdzSIk6/OF9CiNA3QKvETx82oSbC
# 6ILVSui5xrkODYcYiTu04r8XJfuUFhopfyqRlK+1jXLWebSTlyx8N2KaLM5a/mrd
# nzRM3dUZ7X72mH8DoV35t88FR5d4+9+BJFgLFzj569PxZ4/TGH1zKAUZXbql6qH8
# CFmLsp2PZH0Y0xQZh0t6GbYrZoDjvvF6Z28hY3YlygjYo0dkSncLS5Oh/63MCEWY
# v7WxbfxDAx92NIGKjWqMhEbj15FTAIECMOB+44EjCWFUkKCz7FO0fSZdELb7kKaV
# rQi9bruHnOQMUNmdrXAAIgpGxrO8I+u8IHeyCaVXDhvgV7lWB4PI8DHXoHr3
# SIG # End signature block
