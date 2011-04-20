# Quickly hacked up "build" file to create NuGet package

# remove whatever was left from previous build
remove-item -recurse -force  NuGet\lib
remove-item -recurse -force  NuGet\tool

# copy files from a succesfull Visual Studio release build
new-item -itemtype directory  NuGet\lib
new-item -itemtype directory  NuGet\tool

Copy-Item CoffeeScriptHttpHandler\bin\Release\CoffeeScriptHttpHandler.dll  NuGet\lib
Copy-Item CoffeeScriptHttpHandler\bin\Release\CoffeeSharp.dll              NuGet\lib
Copy-Item CoffeeScriptHttpHandler\bin\Release\Jurassic.dll                 NuGet\lib

Copy-Item Coffee\bin\Release\Coffee.exe       NuGet\tool
Copy-Item Coffee\bin\Release\CoffeeSharp.dll  NuGet\tool
Copy-Item Coffee\bin\Release\Jurassic.dll     NuGet\tool

# actual packaging
tools\NuGet.exe pack NuGet\CoffeeSharp.nuspec
