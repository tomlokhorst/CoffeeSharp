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

      var bare = false;

      var s = context.Request.QueryString["bare"];
      if (s != null)
        bool.TryParse(s, out bare);

      var code = File.ReadAllText(path);
      var js = this.coffeeScriptEngine.Compile(code, bare);

      context.Response.ContentType = "text/javascript";
      context.Response.Write(js);
    }

    public bool IsReusable
    {
      get { return true; }
    }
  }
}
