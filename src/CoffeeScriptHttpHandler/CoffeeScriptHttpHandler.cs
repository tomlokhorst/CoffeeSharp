using System.IO.Compression;
using System.Web.Caching;
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
    [ThreadStatic]
    private static CoffeeScriptEngine coffeeScriptEngine;

    private readonly TimeSpan cacheDuration = TimeSpan.FromDays(30);

    public CoffeeScriptHttpHandler()
    {
      if (coffeeScriptEngine == null)
      {
        coffeeScriptEngine = new CoffeeScriptEngine();
      }
    }

    public void ProcessRequest(HttpContext context)
    {
      var request = context.Request;
      var response = context.Response;
      var path = request.PhysicalPath;
      var cache = response.Cache;

      cache.SetExpires(DateTime.Now.Add(cacheDuration));
      cache.SetMaxAge(cacheDuration);
      cache.SetCacheability(HttpCacheability.Public);
      cache.SetValidUntilExpires(false);
      cache.AppendCacheExtension("must-revalidate, proxy-revalidate");

      var lastModified = File.Exists(path)
                           ? File.GetLastWriteTimeUtc(path)
                           : DateTime.UtcNow;

      if (lastModified > DateTime.UtcNow)
      {
        lastModified = DateTime.UtcNow;
      }

      response.Cache.SetLastModified(lastModified);

      var ifModifiedSince = request.Headers["If-Modified-Since"];
      if (ifModifiedSince != null && !isModified(ifModifiedSince, lastModified))
      {
        response.StatusCode = 304;
        return;
      }

      var isCompressed = canGZip(request);
      var bare = getBool(context, "bare");
      var globals = getBool(context, "globals");

      if (!writeFromCache(context, path, isCompressed, bare, globals))
      {
        using (var memoryStream = new MemoryStream(5000))
        {
          using (var writer = isCompressed
                                ? (Stream) (new GZipStream(memoryStream, CompressionMode.Compress))
                                : memoryStream)
          {
            byte[] fileBytes = getFileBytes(path, bare, globals);
            writer.Write( fileBytes, 0, fileBytes.Length );
            writer.Close();
          }

          var responseBytes = memoryStream.ToArray();

          var dep = new CacheDependency(path); 

          context.Cache.Insert(getCacheKey(path, isCompressed, bare, globals),
            responseBytes, dep, Cache.NoAbsoluteExpiration,
            cacheDuration);

          writeBytes( responseBytes, context, isCompressed );
        }
      }
    }

    private bool isModified(string ifModifiedSince, DateTime lastModified)
    {
      var request = DateTime.ParseExact(ifModifiedSince, "r", System.Globalization.CultureInfo.InvariantCulture);
      return lastModified.AddSeconds(-1) > request;
    }

    private bool canGZip(HttpRequest request)
    {
      var acceptEncoding = request.Headers["Accept-Encoding"];
      return !string.IsNullOrEmpty(acceptEncoding)
             && (acceptEncoding.Contains("gzip") || acceptEncoding.Contains("deflate"));
    }

    private byte[] getFileBytes(string path, bool bare, bool globals)
    {
      var text = File.ReadAllText(path);
      text = coffeeScriptEngine.Compile(text, bare, globals);
      var bytes = Encoding.UTF8.GetBytes(text);
      return bytes;
    }

    private string getCacheKey(string path, bool isCompressed, bool bare, bool globals)
    {
      return path + "&c=" + isCompressed + "&b=" + bare + "&g=" + globals;
    }

    private bool writeFromCache(HttpContext context, string path, bool isCompressed, bool bare, bool globals)
    {
      byte[] responseBytes = context.Cache[getCacheKey(path, isCompressed, bare, globals)] as byte[];

      if (null == responseBytes || 0 == responseBytes.Length)
        return false;

      writeBytes(responseBytes, context, isCompressed);
      return true;
    }

    private void writeBytes(byte[] bytes, HttpContext context, bool isCompressed)
    {
      var response = context.Response;

      response.AppendHeader("Content-Length", bytes.Length.ToString());
      response.ContentType = "text/javascript";
      if (isCompressed)
        response.AppendHeader("Content-Encoding", "gzip");

      response.OutputStream.Write(bytes, 0, bytes.Length);
      response.Flush();
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
