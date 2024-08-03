# Script to build the WPF and CLI app, sign the executables and build the installer

Param (
    [switch]$dev
)

function Confirm-Requirements {
    # Check if current directory is the publish directory
    if (-not (Test-Path .\publish.ps1)) {
        Write-Host "Please run the script from the publish directory"
        Exit
    }
    # Check if Inno Setup 6 is installed
    if (-not (Test-Path "C:\Program Files (x86)\Inno Setup 6\ISCC.exe")) {
        Write-Host "Inno Setup 6 is required to build the installer"
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

function Invoke-Tests {
    Set-Location ../Guard.Test
    dotnet test
    Set-Location ../publish
}

function Build-WPF-App {
    Set-Location ../Guard.WPF
    dotnet publish -r win-x64 -c Release --p:PublishSingleFile=true --self-contained true -o bin\publish
    Move-Item bin\publish\2FAGuard.exe ..\publish\bin -Force
    dotnet publish -r win-x64 -c Release --p:PublishSingleFile=true --self-contained true -p:IsPortable=true -o bin\portable
    Move-Item bin\portable\2FAGuard.exe ..\publish\bin\2FAGuard-Portable.exe -Force
    Set-Location ../publish
}

function Build-CLI-App {
    Set-Location ../Guard.CLI
    dotnet publish -r win-x64 -c Release --p:PublishSingleFile=true --self-contained true -o bin\publish
    Move-Item bin\publish\2fa.exe ..\publish\bin -Force
    Set-Location ../publish
}

function Invoke-Code-Signing {
    signtool.exe sign /n "Open Source Developer, Timo Kössler" /t "http://time.certum.pl/" /fd sha256 /d "2FAGuard" /du "https://2faguard.app" .\bin\2FAGuard.exe .\bin\2FAGuard-Portable.exe .\bin\2fa.exe
}

function Build-Installer {
    Start-Process -NoNewWindow -FilePath "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" -ArgumentList "./installer.iss"
}


Write-Host "Checking Requirements"
Confirm-Requirements

Write-Host "Running Tests"
Invoke-Tests

Write-Host "Building WPF App"
Build-WPF-App

Write-Host "Building CLI App"
Build-CLI-App

if ($dev) {
    Write-Host "Dev mode enabled, skipping signing and installer"
    Exit 0
}

Write-Host "Signing Code"
Invoke-Code-Signing

Write-Host "Building Installer"
Build-Installer
Write-Host "Done"
# SIG # Begin signature block
# MIIRbAYJKoZIhvcNAQcCoIIRXTCCEVkCAQExDzANBglghkgBZQMEAgEFADB5Bgor
# BgEEAYI3AgEEoGswaTA0BgorBgEEAYI3AgEeMCYCAwEAAAQQH8w7YFlLCE63JNLG
# KX7zUQIBAAIBAAIBAAIBAAIBADAxMA0GCWCGSAFlAwQCAQUABCAg3TOtaPZgQNrx
# 2LjJrhEyygZtFaf5YDA19SUDqXNvpaCCDaYwgga5MIIEoaADAgECAhEAmaOACiZV
# O2Wr3G6EprPqOTANBgkqhkiG9w0BAQwFADCBgDELMAkGA1UEBhMCUEwxIjAgBgNV
# BAoTGVVuaXpldG8gVGVjaG5vbG9naWVzIFMuQS4xJzAlBgNVBAsTHkNlcnR1bSBD
# ZXJ0aWZpY2F0aW9uIEF1dGhvcml0eTEkMCIGA1UEAxMbQ2VydHVtIFRydXN0ZWQg
# TmV0d29yayBDQSAyMB4XDTIxMDUxOTA1MzIxOFoXDTM2MDUxODA1MzIxOFowVjEL
# MAkGA1UEBhMCUEwxITAfBgNVBAoTGEFzc2VjbyBEYXRhIFN5c3RlbXMgUy5BLjEk
# MCIGA1UEAxMbQ2VydHVtIENvZGUgU2lnbmluZyAyMDIxIENBMIICIjANBgkqhkiG
# 9w0BAQEFAAOCAg8AMIICCgKCAgEAnSPPBDAjO8FGLOczcz5jXXp1ur5cTbq96y34
# vuTmflN4mSAfgLKTvggv24/rWiVGzGxT9YEASVMw1Aj8ewTS4IndU8s7VS5+djSo
# McbvIKck6+hI1shsylP4JyLvmxwLHtSworV9wmjhNd627h27a8RdrT1PH9ud0IF+
# njvMk2xqbNTIPsnWtw3E7DmDoUmDQiYi/ucJ42fcHqBkbbxYDB7SYOouu9Tj1yHI
# ohzuC8KNqfcYf7Z4/iZgkBJ+UFNDcc6zokZ2uJIxWgPWXMEmhu1gMXgv8aGUsRda
# CtVD2bSlbfsq7BiqljjaCun+RJgTgFRCtsuAEw0pG9+FA+yQN9n/kZtMLK+Wo837
# Q4QOZgYqVWQ4x6cM7/G0yswg1ElLlJj6NYKLw9EcBXE7TF3HybZtYvj9lDV2nT8m
# FSkcSkAExzd4prHwYjUXTeZIlVXqj+eaYqoMTpMrfh5MCAOIG5knN4Q/JHuurfTI
# 5XDYO962WZayx7ACFf5ydJpoEowSP07YaBiQ8nXpDkNrUA9g7qf/rCkKbWpQ5bou
# fUnq1UiYPIAHlezf4muJqxqIns/kqld6JVX8cixbd6PzkDpwZo4SlADaCi2JSplK
# ShBSND36E/ENVv8urPS0yOnpG4tIoBGxVCARPCg1BnyMJ4rBJAcOSnAWd18Jx5n8
# 58JSqPECAwEAAaOCAVUwggFRMA8GA1UdEwEB/wQFMAMBAf8wHQYDVR0OBBYEFN10
# XUwA23ufoHTKsW73PMAywHDNMB8GA1UdIwQYMBaAFLahVDkCw6A/joq8+tT4HKbR
# Og79MA4GA1UdDwEB/wQEAwIBBjATBgNVHSUEDDAKBggrBgEFBQcDAzAwBgNVHR8E
# KTAnMCWgI6Ahhh9odHRwOi8vY3JsLmNlcnR1bS5wbC9jdG5jYTIuY3JsMGwGCCsG
# AQUFBwEBBGAwXjAoBggrBgEFBQcwAYYcaHR0cDovL3N1YmNhLm9jc3AtY2VydHVt
# LmNvbTAyBggrBgEFBQcwAoYmaHR0cDovL3JlcG9zaXRvcnkuY2VydHVtLnBsL2N0
# bmNhMi5jZXIwOQYDVR0gBDIwMDAuBgRVHSAAMCYwJAYIKwYBBQUHAgEWGGh0dHA6
# Ly93d3cuY2VydHVtLnBsL0NQUzANBgkqhkiG9w0BAQwFAAOCAgEAdYhYD+WPUCia
# U58Q7EP89DttyZqGYn2XRDhJkL6P+/T0IPZyxfxiXumYlARMgwRzLRUStJl490L9
# 4C9LGF3vjzzH8Jq3iR74BRlkO18J3zIdmCKQa5LyZ48IfICJTZVJeChDUyuQy6rG
# DxLUUAsO0eqeLNhLVsgw6/zOfImNlARKn1FP7o0fTbj8ipNGxHBIutiRsWrhWM2f
# 8pXdd3x2mbJCKKtl2s42g9KUJHEIiLni9ByoqIUul4GblLQigO0ugh7bWRLDm0Cd
# Y9rNLqyA3ahe8WlxVWkxyrQLjH8ItI17RdySaYayX3PhRSC4Am1/7mATwZWwSD+B
# 7eMcZNhpn8zJ+6MTyE6YoEBSRVrs0zFFIHUR08Wk0ikSf+lIe5Iv6RY3/bFAEloM
# U+vUBfSouCReZwSLo8WdrDlPXtR0gicDnytO7eZ5827NS2x7gCBibESYkOh1/w1t
# VxTpV2Na3PR7nxYVlPu1JPoRZCbH86gc96UTvuWiOruWmyOEMLOGGniR+x+zPF/2
# DaGgK2W1eEJfo2qyrBNPvF7wuAyQfiFXLwvWHamoYtPZo0LHuH8X3n9C+xN4YaNj
# t2ywzOr+tKyEVAotnyU9vyEVOaIYMk3IeBrmFnn0gbKeTTyYeEEUz/Qwt4HOUBCr
# W602NCmvO1nm+/80nLy5r0AZvCQxaQ4wggblMIIEzaADAgECAhBbfOwCsxQM/dxh
# 0mcu7vf7MA0GCSqGSIb3DQEBCwUAMFYxCzAJBgNVBAYTAlBMMSEwHwYDVQQKExhB
# c3NlY28gRGF0YSBTeXN0ZW1zIFMuQS4xJDAiBgNVBAMTG0NlcnR1bSBDb2RlIFNp
# Z25pbmcgMjAyMSBDQTAeFw0yNDAzMjgwNjMyMDlaFw0yNTAzMjgwNjMyMDhaMIGK
# MQswCQYDVQQGEwJERTEcMBoGA1UECAwTTm9yZHJoZWluLVdlc3RmYWxlbjEOMAwG
# A1UEBwwFV2VzZWwxHjAcBgNVBAoMFU9wZW4gU291cmNlIERldmVsb3BlcjEtMCsG
# A1UEAwwkT3BlbiBTb3VyY2UgRGV2ZWxvcGVyLCBUaW1vIEvDtnNzbGVyMIICIjAN
# BgkqhkiG9w0BAQEFAAOCAg8AMIICCgKCAgEAn3WhxzmjooxZ4yeBe42K61W+xPQ5
# om7Rlm453ADMpiOEKr3yoAESiOOjXPfUDXML3QLf0K1P5N5b142BfxG8fp7l6HcV
# /jznTA5DgItYYUeaIoNS19imHclTy75Yz26pp+urj8UCS4K8hPqneEuJTrcKl0ce
# IOZ/rnwUiRwqAvl6ojXnEY97vausw2Zmd4OZr4u9Uht6/BK9KOFIOnJU31hASSq9
# Sswo7qQpfuq3VOfEY2VaDQoFySzhuNuls+eInN/suOH3Rfe+B99OHtJ96ha48YsY
# Q7JL/gTy4igR1v257q5MNoS5GMLnEvTz/hwevrEgISR3F+BEMtCjovSOWpm0PPjj
# Rlj0qAVk+qB1jV6DwKQwUo8s7ccX/G7bwMjShARoPNN9Myu5FbNbl+ql8K7ilqY3
# BptpInYJRBBe9Lm3dPv7ExOk/aFOOM9YAtq/e3zQebgVFaHER6om5huQ99S3R2D9
# U0QLEM5zkOtCfNUUDBbdYMAfZo9mGvrrVf42NG2eu2ZZF+2Gi9EoKT6+xrH1k52W
# jGjGFCg49T2CVL0xw76XvtiLdsj4EXX2gIsL7lfPl4aaENRcwOuBtJ9KRjU2mdCq
# 6g6L0WbENGunDovSD3HpKpnmrolDg4SUvzVyywz/sHsrOWDXhiu8J9FzkA4hck5W
# mrqC0rjAkWxr/bUCAwEAAaOCAXgwggF0MAwGA1UdEwEB/wQCMAAwPQYDVR0fBDYw
# NDAyoDCgLoYsaHR0cDovL2Njc2NhMjAyMS5jcmwuY2VydHVtLnBsL2Njc2NhMjAy
# MS5jcmwwcwYIKwYBBQUHAQEEZzBlMCwGCCsGAQUFBzABhiBodHRwOi8vY2NzY2Ey
# MDIxLm9jc3AtY2VydHVtLmNvbTA1BggrBgEFBQcwAoYpaHR0cDovL3JlcG9zaXRv
# cnkuY2VydHVtLnBsL2Njc2NhMjAyMS5jZXIwHwYDVR0jBBgwFoAU3XRdTADbe5+g
# dMqxbvc8wDLAcM0wHQYDVR0OBBYEFEoTqZZIXfm8XhUsGlPCXLm2VY8sMEsGA1Ud
# IAREMEIwCAYGZ4EMAQQBMDYGCyqEaAGG9ncCBQEEMCcwJQYIKwYBBQUHAgEWGWh0
# dHBzOi8vd3d3LmNlcnR1bS5wbC9DUFMwEwYDVR0lBAwwCgYIKwYBBQUHAwMwDgYD
# VR0PAQH/BAQDAgeAMA0GCSqGSIb3DQEBCwUAA4ICAQAB/wMWqolgTjThQvWIXxZf
# Sq1TrOSsPcTn8ZLGJvb41rSsGveYwTHdBS/cD4vOGJB4/Ip/T2T6xr06AncQpU8R
# Tpx1zDUwe+kLud7PksLOFGLbMtyxaMJeHlhWU1xE5LY+geOIQJxJFr1Lkx27aCTG
# RtVLX4rn3/FPB52AkOjywgkaGkxXhsn0bkOXgbhlIRxqQtVUbVPnXbwnqvqzG/Rb
# I6HSW+HleQXzfZU6zPYIKY7PpHQnB7SoCyltDwhXLH+r4ZjFctGlyOMPqnjA6v24
# iBAs8atPL++RYFpYHsTGAO5EOW3TSSbXWr88en/+cXuZnfhaO/oixyNpSrQMLUds
# CB38PUMTF79VfZzHiR7BVJECIQAJ2KluD0x1JE209m+36qj33lBGg54NZJ/VP28B
# pleVFNP7HjCHIYO1jMgVIHoDUlyaEDy4JCsxCVKLGEXhPLVpftdC001986x+Dxjm
# ZTOTkzix1Gq32aCOwqFCqaSdhmEG4ZRjSSJWD5kVGgUFuIJW15qIrHoHyZ5zLSsT
# pREX138Kfsbnn0iQrB9eMQD1dqGhI1QskbbyhvR5HC9FKCnl9bmQWxysg8FiRznr
# +6JbHbSLid0OgJ3c4EJq4gjMXP9NM36aSB9TK7RY80Dz+0kgwCNqHl/TWcZX7Q28
# g24ZIgFGUXrzjJp99FtB2TGCAxwwggMYAgEBMGowVjELMAkGA1UEBhMCUEwxITAf
# BgNVBAoTGEFzc2VjbyBEYXRhIFN5c3RlbXMgUy5BLjEkMCIGA1UEAxMbQ2VydHVt
# IENvZGUgU2lnbmluZyAyMDIxIENBAhBbfOwCsxQM/dxh0mcu7vf7MA0GCWCGSAFl
# AwQCAQUAoIGEMBgGCisGAQQBgjcCAQwxCjAIoAKAAKECgAAwGQYJKoZIhvcNAQkD
# MQwGCisGAQQBgjcCAQQwHAYKKwYBBAGCNwIBCzEOMAwGCisGAQQBgjcCARUwLwYJ
# KoZIhvcNAQkEMSIEIBWSW7H/yOza0yiS4UIc7FtF8anawqV8h/IiEYsrSnYVMA0G
# CSqGSIb3DQEBAQUABIICAGuCkAXmn9ZyDsXCpcExW18qSSGpJiuaKBsot/Y9Gjg4
# qUxZ2wVkZGxwMZ/uSrjb7d5xL87duTBdEta7WMFDWfjiW7SFrRv8VgzkBlSm2Rx7
# exgYuN9wPqY1zvOwHzjlmcmsEUNBKqqrPXuv2oeobPxTfzAYdXF4kjdEO3KVKnDk
# 8tJztKnGOvc7SnsN/1DOPp+s1Yk5oW7BxaVqxr1OIp8Irt4LRp9BmfTVYrdnbAN8
# VkNm57lQndA1pU/ByyJyx2zsJ0tTjipkqN9ABqvo3HGBWwxIpH+7aD2o5CzZSBp1
# m1p5UdBPlcRBNGDPThyCjEseKpUIX4bBRSuKl78SMU+JHC38tceKmryxxdgMCRGU
# +p2cbjfxKQd1uNlNgOwCSwFooAs50T61F7ISiOOr2bY0Ry50hpDtcdzazW6OKTOZ
# kRKVN+bCwyVrFnlx3cONvE0MxByXKTfiy1iq99BK3MqIuFR9Tpepq0jPPr/mFd+S
# gY7Yflsr0DIvTiEP/KHNroZgi9jTFOFeMTg7UQYMYrnn+t0sTDlfRtDJlXveyF98
# dokGN9SxBDrQFhnVGx9u4NRcbXyAlviumCCYl0ihUZux2XxDOsbrr3HPA6wh0JS0
# V3GxTSkzIR+plbBFt51uvLBbQ/lVP5pKBg9bECoHAvBZYlLycBR852AAJaDeqAcs
# SIG # End signature block
