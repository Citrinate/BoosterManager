@echo off
if not exist ArchiSteamFarm\ArchiSteamFarm (git submodule update --init)
if [%1]==[] goto noarg
git submodule foreach "git fetch origin; git checkout %1;"
goto continue
:noarg
git submodule foreach "git fetch origin; git checkout $(git rev-list --tags --max-count=1);"
:continue
git submodule foreach "git describe --tags;"
del .\BoosterCreator\*.zip
dotnet publish -c "Release" -f "net48" -o "out/generic-netf"
rename .\BoosterCreator\BoosterCreator.zip BoosterCreator-netf.zip 
dotnet publish -c "Release" -f "netcoreapp3.1" -o "out/generic" "/p:LinkDuringPublish=false"