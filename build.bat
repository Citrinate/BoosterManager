del .\BoosterCreator\*.zip
dotnet publish -c "Release" -f "net48" -o "out/generic-netf"
rename .\BoosterCreator\BoosterCreator.zip BoosterCreator-netf.zip 
dotnet publish -c "Release" -f "netcoreapp3.1" -o "out/generic" "/p:LinkDuringPublish=false"