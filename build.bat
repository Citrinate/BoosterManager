del .\BoosterCreator\*.zip
dotnet publish -c "Release" -f "net472" -o "out/generic-netf"
rename .\BoosterCreator\BoosterCreator.zip BoosterCreator-netf.zip 
dotnet publish -c "Release" -f "netcoreapp2.2" -o "out/generic" "/p:LinkDuringPublish=false"