CoffeeSharp
===========

.NET bindings to the [CoffeeScript][1] compiler, using the [Jurassic Javascript Compiler][2] internally.

See the [homepage][3] for details on how to install and use this package.


About the code
--------------

The `CoffeeSharp` library project contains the CoffeeScript source code as a resource.
Based on the Jurassic library, it exports the `CoffeeScriptEngine` class, with methods like `Compile` and `Eval`.

The `CoffeeScriptHttpHandler` project contains a single HttpHandler that calls the `CoffeeSharp` library.

The `Coffee` project is a commandline tool that calls the `CoffeeSharp` library.
In contrast to the previous two projects that are written in C#, this project is written in F#.


[1]: http://coffeescript.org/
[2]: http://jurassic.codeplex.com/
[3]: http://tomlokhorst.github.com/CoffeeSharp
