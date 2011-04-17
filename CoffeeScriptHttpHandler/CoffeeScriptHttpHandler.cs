using CoffeeSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace CoffeeSharp
{
  public class CoffeeScriptHttpHandler : IHttpHandler
  {
    private CoffeeScriptEngine coffeeScriptEngine;

    public CoffeeScriptHttpHandler()
    {
      this.coffeeScriptEngine = new CoffeeScriptEngine();
    }

    public void ProcessRequest(HttpContext context)
    {
      var path = context.Request.PhysicalPath;

      var bare = getBool(context, "bare");
      var globals = getBool(context, "globals");

      var code = File.ReadAllText(path);
      var js = this.coffeeScriptEngine.Compile(code, bare, globals);

      context.Response.ContentType = "text/javascript";
      context.Response.Write(js);
    }

    private bool getBool(HttpContext context, string name)
    {
      var b = false;
      var s = context.Request.QueryString[name];
      if (s != null)
        bool.TryParse(s, out b);

      return b;
    }

    public bool IsReusable
    {
      get { return true; }
    }
  }
}
