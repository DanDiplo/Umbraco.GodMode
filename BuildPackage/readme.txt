Edit \BuildPackage\Package.build.xml and change BasePackageVersion to the version you wish to create (must be in the format 1.2.3.4) 

The last digit is the build number and not used for the product version.

Execute \BuildPackage\build.bat - this expects you to have your Umbraco solution in a folder called Umbraco8Sql (it then copies package content to UmbracoContent folder)

Check \BuildPackage\Package to find both the Umbraco and NuGet package files ready to roll

If you ever need to change the Umbraco.Core dependency, you will need to update the version in both \BuildPackage\Package.build.xml & \BuildPackage\package.nuspec