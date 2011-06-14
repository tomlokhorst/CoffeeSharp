using System;
using System.IO;
using System.Web;
using System.Web.Caching;
using CoffeeSharp;
using System.Threading;

namespace CoffeeSharp
{
  public class CoffeeScriptHttpHandler : IHttpHandler
  {
    private ProcessorItem coffeeScriptProcInfo;

    public CoffeeScriptHttpHandler()
    {
        coffeeScriptProcInfo = new ProcessorItem(ProcessCoffee, "application/javascript");
    }

    public void ProcessRequest(HttpContext context)
    {
        ProcessRequest(new HttpContextWrapper(context));
    }

    public void ProcessRequest(HttpContextBase context)
    {
        var path = context.Request.PhysicalPath;
        DateTime lastWrite = File.GetLastWriteTime(path);
        Cache cache = context.Cache;

        var cachedItem = cache[path] as TransformedCacheItem;
        if (cachedItem == null || cachedItem.TimeStamp < lastWrite)
        {
            var result = coffeeScriptProcInfo.Processor(path, context);
            cache[path] = cachedItem = new TransformedCacheItem(lastWrite, coffeeScriptProcInfo.MimeType, result);
        }

        context.Response.ContentType = cachedItem.MimeType;
        context.Response.Write(cachedItem.Text);
    }

    private CoffeeScriptEngine GetCoffeeScriptEngine()
    {
        CoffeeScriptEngine cse = null;

        // Ugly, but the stackoverflow exception in ASP.Net + Jurrassic reqs this
        ThreadStart threadAction = () =>
        {
            cse = new CoffeeScriptEngine();
        };
        var thread = new Thread(threadAction, 1024 * 1024 * 4);
        thread.Start();
        thread.Join();
        return cse;
    }

    private bool getBool(HttpContextBase context, string name)
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
        
    public string ProcessCoffee(string coffeeScriptPath, HttpContextBase context)
    {
        string jsPath = Path.ChangeExtension(coffeeScriptPath, "js");
        string compiledJS = null;
        if (File.Exists(jsPath) && File.GetLastWriteTime(jsPath) > File.GetLastWriteTime(coffeeScriptPath))
        {
            compiledJS = File.ReadAllText(jsPath);
        }
        else
        {
            var bare = getBool(context, "bare");
            var globals = getBool(context, "globals");
            var code = File.ReadAllText(coffeeScriptPath);

            CoffeeScriptEngine coffeeScriptEngine = (CoffeeScriptEngine)context.Cache["CoffeeScriptEngine"] ?? GetCoffeeScriptEngine();
            context.Cache["CoffeeScriptEngine"] = coffeeScriptEngine;
            lock(coffeeScriptEngine)
            {
                compiledJS = coffeeScriptEngine.Compile(code, bare, globals);
            }
        }

        return (compiledJS == string.Empty) ? null : compiledJS;
    }

    public class ProcessorItem
    {
        public ProcessorItem()
        {
        }

        public ProcessorItem(Func<string, HttpContextBase, string> processor, string mimeType)
        {
            Processor = processor;
            MimeType = mimeType;
        }

        public Func<string, HttpContextBase, string> Processor { get; set; }
        public string MimeType { get; set; }
    }

    public class TransformedCacheItem
    {
        public TransformedCacheItem(DateTime timeStamp, string mimeType, string text)
        {
            TimeStamp = timeStamp;
            MimeType = mimeType;
            Text = text;
        }

        public DateTime TimeStamp { get; set; }
        public string MimeType { get; set; }
        public string Text { get; set; }
    }
  }
}
