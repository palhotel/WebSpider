using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

namespace WebSpider
{
    class RequestState
    {
        public int depth;
        public HttpWebRequest request;
    }

    class Program
    {
        private string url;
        private int depth;
        private Dictionary<string, int> unload = new Dictionary<string, int>();
        private Dictionary<string, int> loaded = new Dictionary<string, int>();

        private static ManualResetEvent allDone = new ManualResetEvent(false);

        public Program() {

        }

        public Program(string url, int depth) {
            this.url = url;
            this.depth = depth;
        }

        public void RunSpider() {
            unload.Add(url, 0);
            while (unload.Count > 0) {
                var target = unload.First();
                var tempUrl = target.Key;
                var tempDepth = target.Value;
                unload.Remove(tempUrl);
                loaded.Add(tempUrl, loaded.Count + 1); 
                allDone.Reset();
                if (tempDepth <= this.depth)
                {
                    Console.WriteLine("#:" + tempUrl + "# depth:" + tempDepth.ToString());
                    HttpWebRequest req = (HttpWebRequest) WebRequest.Create(tempUrl);
                    IAsyncResult ar = req.BeginGetResponse(HandleResponse,
                        new RequestState() {depth = tempDepth, request = req});
                    allDone.WaitOne();
                }
            }
        }

        public void HandleResponse(IAsyncResult ar) {
            try {
                var res = ((RequestState)ar.AsyncState).request.EndGetResponse(ar);
                var thisDepth = ((RequestState)ar.AsyncState).depth;
                using (StreamReader sr = new StreamReader(res.GetResponseStream())) {
                    string html = sr.ReadToEnd();
                    const string pattern = @"http://([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)?";
                    Regex r = new Regex(pattern, RegexOptions.IgnoreCase);
                    MatchCollection m = r.Matches(html);
                    for (int i = 0; i < m.Count; i++) {
                        var thisUrl = m[i].ToString();
                        if (!unload.ContainsKey(thisUrl) && !loaded.ContainsKey(thisUrl)) {
                            if (thisUrl.EndsWith(".html") || thisUrl.EndsWith(".htm") || thisUrl.EndsWith(".php") ||
                                thisUrl.EndsWith("/")) {
                                    unload.Add(m[i].ToString(), thisDepth + 1);
                            }
                        }
                    }
                }
            }
            catch (Exception e) {

            }
            finally {
                allDone.Set();
            }


        }

        static void Main(string[] args) {
            string url;
            int depth;

            ScanArgs(args, out url, out depth);
            new Program(url, depth).RunSpider();
            Console.WriteLine("Bye,success");
        }

        public static void ShowUsage() {
            Console.WriteLine("DownLoad a WebSite! example:\nWebSpider -url http://www.likeada.com/ -depth 3");
        }

        public static void ScanArgs(string[] args, out string url, out int depth) {
            url = "http://www.likeada.com/";
            depth = 3;
            if (args.Length <= 0) {
                ShowUsage();
            } else {
                try {
                    for (int i = 0; i < args.Length - 1; i++) {
                        if (args[i] == "-url" && !string.IsNullOrEmpty(args[i + 1])) {
                            url = args[i + 1];
                        } else if (args[i] == "-depth" && !string.IsNullOrEmpty(args[i + 1])) {
                            depth = int.Parse(args[i + 1]);
                        }
                    }
                }
                catch (Exception e) {
                    ShowUsage();
                }

            }
        }
    }
}
