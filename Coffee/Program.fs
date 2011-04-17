[<assembly: System.Reflection.AssemblyVersion("1.0.1.1")>]
do ()

let toNull (o : 'T option) =
  match o with
    | None   -> null
    | Some v -> v

let toOption o =
  match o with
    | null -> None
    | _    -> Some o

let printException (f : Lazy<'a>) : 'a option =
  try
    f.Force() |> Some
  with
    | ex -> eprintfn "%s\n%s" ex.Message ex.StackTrace
            None

open CoffeeSharp
open OptionParser

let cse = new CoffeeScriptEngine()

let showVersion () =
  printfn "CoffeeScript version %s" "1.0.1"

let showHelp () =
  printfn "Usage: Coffee.exe [options] path\\to\\script.coffee"
  printfn ""
  for s in helps options do
    printfn "%s" s
  printfn ""

let eval code bare globals filename =
  let c = lazy (cse.Eval (code, bare, globals, toNull filename))
  printException c

let rec repl () =
  printf "coffee> "
  let line = System.Console.ReadLine ()
  Option.iter (printfn "%s") (eval line true true (Some "repl"))
  repl ()

open System.IO

let compile code bare globals filename =
  let c = lazy (cse.Compile (code, bare, globals, toNull filename))
  printException c

let compileScripts sources bare print outputDir =
  let c fn code = compile code bare false (Some fn)
  let filename dir fn =
    let fn'  = Path.Combine(Option.fold (fun _ s -> s) dir outputDir, fn)
    let dir' = Path.GetDirectoryName fn'
    let file = Path.GetFileNameWithoutExtension fn'
    Directory.CreateDirectory dir' |> ignore
    Path.Combine(dir', file + ".js")
  let output dir fn s =
    if print
    then printfn "%s" s
    else File.WriteAllText(filename dir fn, s)
  let f (dir, fns) =
    List.map (fun fn -> Path.Combine(dir, fn) |> File.ReadAllText |> c fn |> Option.iter (output dir fn))
             fns
  List.map f sources |> ignore

let files (src : string) : string * string list =
  let rec files' root path =
    let path' = Path.Combine(root, path)
    if not (Directory.Exists path')
    then raise (FileNotFoundException path')
    else let di = new DirectoryInfo(path')
         let fs = di.GetFiles("*.coffee") |> Array.toList
         let ds = di.GetDirectories() |> Array.toList
         let dir s = Path.Combine(path, s)
         List.append
           (List.map (fun (fi : FileInfo) -> dir fi.Name) fs)
           (List.collect (fun (di : DirectoryInfo) -> files' root (dir di.Name)) ds)

  if File.Exists src
  then (Path.GetDirectoryName src, [Path.GetFileName src])
  else (src, files' src "")

let tokens code =
  let c =
    lazy (let ts = cse.Tokens code
          for (t, v) in ts do
            printf "[%s %s] " t (v.ToString().Replace("\n", "\\n"))
          printfn ""
         )
  printException c |> ignore

let nodes code =
  let c = lazy (printfn "%s" (cse.Nodes code))
  printException c |> ignore

[<EntryPoint>]
let main args =
  let config = args |> List.ofArray |> parseOptions |> toLong |> parse defaultConfig
  let stdin () = (new StreamReader (System.Console.OpenStandardInput())).ReadToEnd()
  let sources f src =
    let (dir, fns) = files src
    List.map (fun fn -> Path.Combine(dir, fn) |> File.ReadAllText |> f) fns
  match config.action with
    | Version      -> showVersion()
    | Help         -> showHelp()
    | Interactive  -> repl()
    | Eval    code -> if config.stdio
                      then eval (stdin()) false false (Some "stdin") |> ignore
                      else Option.iter (fun c -> eval c false false None |> ignore) code
    | Compile srcs -> if config.stdio
                      then compile (stdin()) config.bare false None |> Option.iter (printfn "%s")
                      else compileScripts (List.map files srcs) config.bare config.print config.outputDir
    | Tokens  osrc -> if config.stdio
                      then stdin() |> tokens
                      else Option.iter (sources tokens >> ignore) osrc
    | Nodes   osrc -> if config.stdio
                      then nodes (stdin())
                      else Option.iter (sources nodes >> ignore) osrc
  0
