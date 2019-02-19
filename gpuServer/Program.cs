using System;
using System.Net;
using System.Threading;
using System.Linq;
using System.Text;
using SimpleWebServer;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace SimpleWebServer
{
    
    public class ResponsePair
    {
     public Uri[] Prefixes {get;private set;}
     public Func<HttpListenerRequest,string> ResponseMethod {get;private set;}
     
     public HttpListener listener{get;set;}
     
     public ResponsePair(Uri[] prefixes, Func<HttpListenerRequest,string> method )
     {
         Prefixes = prefixes;
         ResponseMethod = method;
          if (prefixes == null || prefixes.Length == 0)
                throw new ArgumentException("prefixes");

            // A responder method is required
            if (method == null)
                throw new ArgumentException("method");
     }
     
    }
    
    public class WebServer
    {
        private readonly List<ResponsePair> Routes = new List<ResponsePair>();
        public WebServer(List<ResponsePair> routes )
        {
            if (!HttpListener.IsSupported)
                throw new NotSupportedException(
                    "Needs Windows XP SP2, Server 2003 or later.");

           Routes = routes;
            foreach (var route in Routes){
                
                route.listener = new HttpListener();
                foreach (var prefix in route.Prefixes)
                {
                    route.listener.Prefixes.Add(prefix.AbsoluteUri);
                }
                route.listener.Start();
            }
        }

        public async void Run()
        {
            var tasks = new List<System.Threading.Tasks.Task<Tuple<HttpListenerContext,ResponsePair>>>();
            
          var handler =  new Action<Task<Tuple<System.Net.HttpListenerContext,ResponsePair>>> ( (ctxTask) => {  
             var ctxtup = ctxTask.Result;
             var ctx = ctxtup.Item1;
            var route = ctxtup.Item2;
             Console.WriteLine("we are handling a request to" + ctx.Request.Url.ToString());
              try
                            {
                                
                                var methodToRespondWith = route.ResponseMethod;
                                string rstr = methodToRespondWith(ctx.Request);
                                byte[] buf = Encoding.UTF8.GetBytes(rstr);
                                ctx.Response.ContentLength64 = buf.Length;
                                ctx.Response.OutputStream.Write(buf, 0, buf.Length);
                            }
                            catch(Exception e)  { Console.WriteLine(e); } // suppress any exceptions
                            finally
                            {
                                // always close the stream
                                ctx.Response.OutputStream.Close();
                                tasks.Remove(ctxTask);
                            }} );
            
            
                Console.WriteLine("Webserver running...");
                try
                {
                    
                  while (Routes.All(x=>x.listener.IsListening))
                    {
                    
                    //foreach route in the all routes start async task
                    foreach (var route in Routes )
                    {
                      tasks.Add(route.listener.GetContextAsync().ContinueWith((ctask) => {return Tuple.Create(ctask.Result,route);}) );
                      }
                    
                      Console.WriteLine(tasks.Count);
                      
                        var ctxtask = await System.Threading.Tasks.Task.WhenAny(tasks);
                      handler(ctxtask);
                    }
                        
                    }
                catch(Exception e) {Console.WriteLine(e.Message);} // suppress any exception
        }

        public void Stop()
        {
              foreach (var listener in Routes.Select(x=>x.listener).ToList())
                    {
            listener.Stop();
            listener.Close();
                    }
        }
    }
}

class Program
{
    static void Main(string[] args)
    {
        
       var routes = new List<ResponsePair>();
       routes.Add(new ResponsePair(new Uri[] {new Uri("http://localhost:8080/index/")},SendResponse));
       routes.Add(new ResponsePair(new Uri[] {new Uri("http://localhost:8080/test/")},SendResponse));

       
        WebServer ws = new WebServer(routes);
        ws.Run();
        Console.WriteLine("A simple webserver. Press a key to quit.");
        Console.ReadKey();
        ws.Stop();
    }

    public static string SendResponse(HttpListenerRequest request)
    {
        return string.Format("<HTML><BODY>My web page.<br>{0}</BODY></HTML>", DateTime.Now);
    }
}