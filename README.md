CoffeeSharp
===========

.NET bindings to the [CoffeeScript][1] compiler, using the [Jurassic Javascript Compiler][2] internally.

Getting started
---------------

The download CoffeeSharp-1.0.1.0.zip contains the following:

  - `Coffee.exe` Command line compiler, comparable to the node.js based `coffee` executable, but not as full-featured.
  - `CoffeeScriptHttpHandler.dll` HttpHandler for use within an ASP.NET application.

    Configure as such:

        <httpHandlers>
          <add verb="*" path="*.coffee" type="CoffeeSharp.CoffeeScriptHttpHandler, CofeeScriptHttpHandler" />
        </httpHandlers>

  - `CoffeeSharp.dll` Class library, exports the `CoffeeSharp.CoffeeScriptEngine` class.
  - `Jurassic.dll` Required external library for JavaScript evaluation.

[1]: http://coffeescript.org/
[2]: http://jurassic.codeplex.com/
