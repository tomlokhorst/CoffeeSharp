module OptionParser

type Action =
  | Compile      of string list
  | Interactive
  | Eval         of string option
  | Tokens       of string option
  | Nodes        of string option
  | Version
  | Help

type Config =
  { action      : Action;
    outputDir   : string option;
    joinFiles   : bool;
    watch       : bool;
    print       : bool;
    stdio       : bool;
    bare        : bool;
    arguments   : string list;
  }

let defaultConfig =
  { action    = Interactive;
    outputDir = None;
    joinFiles = false;
    watch     = false;
    print     = false;
    stdio     = false;
    bare      = false;
    arguments = [];
  }

let options =
  [ (Some 'c', "compile"     , "compile to JavaScript and save as .js files");
    (Some 'i', "interactive" , "run an interactive CoffeeScript REPL");
    (Some 'o', "output"      , "set the directory for compiled JavaScript");
    (Some 'j', "join"        , "concatenate the scripts before compiling");
    (Some 'w', "watch"       , "watch scripts for changes, and recompile");
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

type Option =
  | Short of char
  | Long  of string

let rec parseOptions args =
  match args with
    | []                  -> ([], [], [])
    | "--" :: rest        -> ([], [], rest)
    | Prefix "--" s::rest -> let (xs, ys, zs) = parseOptions rest
                             (Long s :: xs, ys, zs)
    | Prefix "-" s::rest  -> let (xs, ys, zs) = parseOptions rest
                             (List.append (List.map Short (Seq.toList s)) xs, ys, zs)
    | s :: rest           -> let (xs, ys, zs) = parseOptions rest
                             (xs, s :: ys, zs)

let toLong (opts, args, rest) =
  let f c = List.collect (fun (x, y, _) -> if x = Some c then [y] else []) options
  let g o = match o with Short c -> f c | Long s -> [s]
  (List.collect g opts, args, rest)

let rec parse cfg args =
  match args with
    | ([]                   , args   , rest) -> {       cfg                    with arguments = rest }
    | ("compile"     :: opts, args   , rest) -> { parse cfg (opts, []  , rest) with action    = Compile args }
    | ("interactive" :: opts, args   , rest) -> { parse cfg (opts, args, rest) with action    = Interactive }
    | ("output"      :: opts, []     , rest) -> { parse cfg (opts, []  , rest) with outputDir = None }
    | ("output"      :: opts, a::args, rest) -> { parse cfg (opts, args, rest) with outputDir = Some a }
    | ("watch"       :: opts, args   , rest) -> { parse cfg (opts, args, rest) with watch     = true }
    | ("join"        :: opts, args   , rest) -> { parse cfg (opts, args, rest) with joinFiles = true }
    | ("print"       :: opts, args   , rest) -> { parse cfg (opts, args, rest) with print     = true }
    | ("stdio"       :: opts, args   , rest) -> { parse cfg (opts, args, rest) with stdio     = true }
    | ("eval"        :: opts, []     , rest) -> { parse cfg (opts, []  , rest) with action    = Eval None }
    | ("eval"        :: opts, a::args, rest) -> { parse cfg (opts, args, rest) with action    = Eval (Some a) }
    | ("bare"        :: opts, args   , rest) -> { parse cfg (opts, args, rest) with bare      = true }
    | ("tokens"      :: opts, []     , rest) -> { parse cfg (opts, []  , rest) with action    = Tokens None }
    | ("tokens"      :: opts, a::args, rest) -> { parse cfg (opts, args, rest) with action    = Tokens (Some a) }
    | ("nodes"       :: opts, []     , rest) -> { parse cfg (opts, []  , rest) with action    = Nodes None }
    | ("nodes"       :: opts, a::args, rest) -> { parse cfg (opts, args, rest) with action    = Nodes (Some a) }
    | ("version"     :: opts, args   , rest) -> { parse cfg (opts, args, rest) with action    = Version }
    | ("help"        :: opts, args   , rest) -> { parse cfg (opts, args, rest) with action    = Help }
    | (_             :: opts, args   , rest) ->   parse cfg (opts, args, rest)