module OptionParser

type Action =
  | Compile
  | Interactive
  | Eval
  | Tokens
  | Nodes
  | Version
  | Help

type Config =
  { action      : Action;
  //  outputDir   : string option;
  //  joinFiles   : bool;
  //  watchDir    : bool;
    print       : bool;
    stdio       : bool;
    bare        : bool;
    filename    : string option;
    code        : string option;
  }

let defaultConfig =
  { action    = Eval;
  //  outputDir = None;
  //  joinFiles = false;
  //  watchDir  = false;
    print     = false;
    stdio     = false;
    bare      = false;
    filename  = None;
    code      = None;
  }

let options =
  [ (Some 'c', "compile"     , "compile to JavaScript and save as .js files");
    (Some 'i', "interactive" , "run an interactive CoffeeScript REPL");
  //  (Some 'o', "output"      , "set the directory for compiled JavaScript");
  //  (Some 'j', "join"        , "concatenate the scripts before compiling");
  //  (Some 'w', "watch"       , "watch scripts for changes, and recompile");
    (Some 'p', "print"       , "print the compiled JavaScript to stdout");
  //  (Some 'l', "lint"        , "pipe the compiled JavaScript through JSLint");
    (Some 's', "stdio"       , "listen for and compile scripts over stdio");
    (Some 'e', "eval"        , "compile a string from the command line");
  //  (Some 'r', "require"     , "require a library before executing your script");
    (Some 'b', "bare"        , "compile without the top-level function wrapper");
    (Some 't', "tokens"      , "print the tokens that the lexer produces");
    (Some 'n', "nodes"       , "print the parse tree that Jison produces");
  //  (None    , "nodejs"      , "pass options through to the \"node\" binary");
    (Some 'v', "version"     , "display CoffeeScript version");
    (Some 'h', "help"        , "display this help message");
  ]

let helps options =
  let help maxWidth (optionChar, name, desc) =
      Option.fold (fun _ c -> "  -" + new string [|c|]) "    " optionChar
    + ", --" + name + String.replicate (maxWidth - name.Length) " "
    + "  " + desc
  List.map (help (options |> List.map (fun (_, s : string, _) -> s.Length) |> List.max)) options

let (|Prefix|_|) (p:string) (s:string) =
  if s.StartsWith(p)
  then Some(s.Substring(p.Length))
  else None

let expand s =
  let f c = List.collect (fun (x, y, _) -> if x = Some c then [y] else []) options
  match s with
    | Prefix "--" rest -> [ rest ]
    | Prefix "-" rest  -> List.collect f (Seq.toList rest)
    | s                -> [ s ]

let parse' cfg arg =
  match arg with
    | "compile"     -> { cfg with action = Compile }
    | "interactive" -> { cfg with action = Interactive }
    | "print"       -> { cfg with print  = true }
    | "stdio"       -> { cfg with stdio  = true }
    | "eval"        -> { cfg with action = Eval }
    | "bare"        -> { cfg with bare   = true }
    | "tokens"      -> { cfg with action = Tokens }
    | "nodes"       -> { cfg with action = Nodes }
    | "version"     -> { cfg with action = Version }
    | "help"        -> { cfg with action = Help }
    | s             -> if cfg.action = Eval && cfg.code = None
                       then { cfg with code     = Some s }
                       else if cfg.filename = None
                            then { cfg with filename = Some s }
                            else cfg

let parse args = List.fold parse' defaultConfig (List.collect expand args)

let cleanup cfg =
  if cfg.filename.IsSome || cfg.stdio
  then cfg
  else match cfg.action with
        | Compile | Tokens  | Nodes -> { cfg with action = Interactive; }
        | _ -> cfg
