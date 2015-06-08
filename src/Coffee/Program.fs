[<assembly: System.Reflection.AssemblyVersion("0.6.0.0")>]
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

open System.Reflection

let showVersion () =
  printfn "CoffeeSharp  version %s" (Assembly.GetExecutingAssembly().GetName().Version.ToString())
  printfn "CoffeeScript version %s" "1.9.3"

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

let compileScript dir fn bare print outputDir log =
  let c fn code = compile code bare false (Some fn)
  
  let read fn = 
    use file = File.Open(fn, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
    use reader = new StreamReader(file)
    reader.ReadToEnd()

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
  Path.Combine(dir, fn)
  |> read
  |> c fn
  |> Option.iter (output dir fn)
  if log
  then printfn "%s - compiled %s" (System.DateTime.Now.ToLongTimeString()) fn
  else ()

let compileScripts sources bare print outputDir log =
  List.map (fun (dir, fns) -> List.map (fun fn -> compileScript dir fn bare print outputDir log) fns) sources
  |> ignore

let watch (dir, fns) bare print outputDir =
  let onChange (e: FileSystemEventArgs) =
    if List.exists (fun i -> i = e.Name) fns
    then compileScript dir e.Name bare print outputDir true
    else ()
  let fsw = (new FileSystemWatcher(dir))
  fsw.IncludeSubdirectories <- true
  fsw.Changed.Add onChange
  fsw.EnableRaisingEvents <- true
  let rec loop () =
    System.Console.ReadLine () |> ignore
    loop ()
  loop ()

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
  then let dir  = Path.GetDirectoryName src
       let dir' = if dir = "" then "." else dir
       (dir', [Path.GetFileName src])
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
                      else if code = None && not config.arguments.IsEmpty
                           then let filename = config.arguments.Head
                                let code     = File.ReadAllText filename
                                eval code false false (Some filename) |> ignore
                           else repl()
    | Compile srcs -> if config.stdio
                      then compile (stdin()) config.bare false None |> Option.iter (printfn "%s")
                      else compileScripts (List.map files srcs) config.bare config.print config.outputDir config.watch
                           if config.watch
                           then List.map (fun src -> watch (files src) config.bare config.print config.outputDir) srcs
                                |> ignore
                           else ()

    | Tokens  osrc -> if config.stdio
                      then stdin() |> tokens
                      else Option.iter (sources tokens >> ignore) osrc
    | Nodes   osrc -> if config.stdio
                      then nodes (stdin())
                      else Option.iter (sources nodes >> ignore) osrc
  0
