open CoffeeSharp

let cse = new CoffeeScriptEngine()

let main =
  cse.Eval "console.log 'Hello, World!'"
