using Jurassic;
using Jurassic.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CoffeeSharp
{
  public class CoffeeScriptEngine
  {
    private ScriptEngine scriptEngine;

    public CoffeeScriptEngine()
    {
      this.scriptEngine = new ScriptEngine();
      scriptEngine.Execute(Resources.CoffeeScriptSource);
      scriptEngine.Execute("coffeeScriptEval = CoffeeScript.eval;");
      scriptEngine.Execute("coffeeScriptNodes = CoffeeScript.nodes;");
      scriptEngine.Execute("coffeeScriptTokens = CoffeeScript.tokens;");
      scriptEngine.Execute("coffeeScriptCompile = CoffeeScript.compile;");
    }

    public string Eval(string code, bool bare = false, bool globals = false, string filename = "repl")
    {
      var options = new JsObject(scriptEngine);
      options["bare"]     = bare;
      options["globals"]  = globals;
      options["filename"] = filename;

      scriptEngine.SetGlobalValue("console", new Jurassic.Library.FirebugConsole(scriptEngine));
      var o = scriptEngine.CallGlobalFunction("coffeeScriptEval", code, options);
      scriptEngine.Evaluate("delete console");

      return o == null || o is Undefined ? null : o.ToString();
    }

    public string Nodes(string source)
    {
      return scriptEngine.CallGlobalFunction("coffeeScriptNodes", source).ToString();
    }

    public IEnumerable<Tuple<String, object>> Tokens(string code, bool rewrite = true)
    {
      var options = new JsObject(scriptEngine);
      options["rewrite"] = rewrite;

      var ts = (ArrayInstance)scriptEngine.CallGlobalFunction("coffeeScriptTokens", code, options);

      foreach (var v in ts.ElementValues.Cast<ArrayInstance>())
        yield return Tuple.Create((string)v[0], v[1]);
    }

    public string Compile(string code, bool bare = false)
    {
      var options = new JsObject(scriptEngine);
      options["bare"] = bare;

      return scriptEngine.CallGlobalFunction<string>("coffeeScriptCompile", code, options);
    }

    private class JsObject : ObjectInstance
    {
      public JsObject(ScriptEngine se) : base(se) { }
    }
  }
}
