# Quickly hacked up "build" file to create NuGet package

# copy files from a succesfull Visual Studio release build
new-item -itemtype directory  NuGet\content
new-item -itemtype directory  NuGet\content\Scripts
new-item -itemtype directory  NuGet\lib
new-item -itemtype directory  NuGet\tool

Copy-Item src\web.config.transform  NuGet\content
Copy-Item src\Scripts\MakeCoffee.tt  NuGet\content\Scripts

Copy-Item src\CoffeeScriptHttpHandler\bin\Release\CoffeeScriptHttpHandler.dll  NuGet\lib
Copy-Item src\CoffeeScriptHttpHandler\bin\Release\CoffeeSharp.dll              NuGet\lib
Copy-Item src\CoffeeScriptHttpHandler\bin\Release\Jurassic.dll                 NuGet\lib

Copy-Item src\Coffee\bin\Release\Coffee.exe       NuGet\tool
Copy-Item src\Coffee\bin\Release\CoffeeSharp.dll  NuGet\tool
Copy-Item src\Coffee\bin\Release\Jurassic.dll     NuGet\tool

Copy-Item src\CoffeeSharp.nuspec  NuGet

# actual packaging
tools\NuGet.exe pack NuGet\CoffeeSharp.nuspec

# cleanup
remove-item -recurse -force  NuGet
