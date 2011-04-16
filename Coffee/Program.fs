[<assembly: System.Reflection.AssemblyVersion("1.0.1.0")>]
do ()

open CoffeeSharp
open OptionParser

let cse = new CoffeeScriptEngine()

let showHelp () =
  printfn "Usage: Coffee.exe [options] path\\to\\script.coffee"
  printfn ""
  for s in helps options do
    printfn "%s" s
  printfn ""

let eval code bare globals =
  try
    let s = cse.Eval (code, bare, globals)
    match s with
      | null -> None
      | _    -> Some s
  with
    | ex -> eprintfn "%s\n%s" ex.Message ex.StackTrace
            None

let rec repl () =
  printf "coffee> "
  let line = System.Console.ReadLine ()
  Option.iter (printfn "%s") (eval line true true)
  repl ()

open System.IO

let compile code filename bare =
  let output = cse.Compile (code, bare)
  match filename with
    | None    -> printfn "%s" output
    | Some fn -> let outFilename = Path.GetFileNameWithoutExtension fn + ".js"
                 File.WriteAllText (outFilename, output)

let tokens code =
  let ts = cse.Tokens code
  for (t, v) in ts do
    printf "[%s %s] " t (v.ToString().Replace("\n", "\\n"))
  printfn ""

let nodes code =
  printfn "%s" (cse.Nodes code)

[<EntryPoint>]
let main args =
  let config = args |> List.ofArray |> parse |> cleanup
  let code () =
    if config.stdio || config.filename.IsNone
    then (new StreamReader (System.Console.OpenStandardInput ())).ReadToEnd ()
    else File.ReadAllText config.filename.Value
  match config.action with
    | Version     -> printfn "CoffeeScript version %s" "1.0.1"
    | Help        -> showHelp ()
    | Interactive -> repl ()
    | Eval        -> Option.iter (fun code -> eval code false false |> ignore) config.code
    | Compile     -> compile (code ()) (if config.stdio || config.print then None else config.filename) config.bare
    | Tokens      -> tokens (code ())
    | Nodes       -> nodes (code ())
  0