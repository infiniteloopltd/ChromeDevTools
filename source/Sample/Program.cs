using MasterDevs.ChromeDevTools.Protocol.Chrome.Page;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MasterDevs.ChromeDevTools.Protocol.Chrome.DOM;
using MasterDevs.ChromeDevTools.Protocol.Chrome.Runtime;
using Task = System.Threading.Tasks.Task;

namespace MasterDevs.ChromeDevTools.Sample
{
    internal class Program
    {
        private const string username = "you@gmail.com";
        private const string password = "some password";

        private static void Main(string[] args)
        {
            Task.Run(async () =>
            {
      
                var chromeProcessFactory = new ChromeProcessFactory(new StubbornDirectoryCleaner());
                // The true/false here means that it's headless or not.
                // Note: If this does not exit cleanly, then you have to manually kill chrome.
                using (var chromeProcess = chromeProcessFactory.Create(9222, false))
                {
                    // STEP 2 - Create a debugging session
                    var sessionInfo = (await chromeProcess.GetSessionInfo()).LastOrDefault();
                    var chromeSessionFactory = new ChromeSessionFactory();
                    var chromeSession = chromeSessionFactory.Create(sessionInfo.WebSocketDebuggerUrl);
                    
                    var navigateResponse = await chromeSession.SendAsync(new NavigateCommand
                    {
                        Url = "https://accounts.google.com/signin/v2/identifier?continue=https%3A%2F%2Fmail.google.com%2Fmail%2F&service=mail&sacu=1&rip=1&flowName=GlifWebSignIn&flowEntry=ServiceLogin"
                    });
                    Console.WriteLine("NavigateResponse: " + navigateResponse.Id);

                    var pageEnableResult = await chromeSession.SendAsync<Protocol.Chrome.Page.EnableCommand>();
                    Console.WriteLine("PageEnable: " + pageEnableResult.Id);


                    WaitForEvent<LoadEventFiredEvent>(chromeSession);

                    Console.WriteLine("LoadEventFiredEvent");

                    var setUsername =
                        $@"document.getElementById(""identifierId"").value = ""{username}"";";

                    await ExecuteJavascript(chromeSession, setUsername);

                    Console.WriteLine("Set Username");

                    const string clickButton = @"document.getElementsByTagName(""button"")[2].click()";

                    await ExecuteJavascript(chromeSession, clickButton);

                    Console.WriteLine("Clicked button");

                    WaitForEvent<FrameStoppedLoadingEvent>(chromeSession);

                    Console.WriteLine("Next frame loaded");
                    

                    var enterPassword = $@"document.getElementsByName(""password"")[0].value=""{password}"";";

                    await ExecuteJavascript(chromeSession, enterPassword);

                    Console.WriteLine("Entered password");
                    
                    const string loginButtonClick = @"document.getElementsByTagName(""button"")[1].click()";

                    await ExecuteJavascript(chromeSession, loginButtonClick);

                    Console.WriteLine("Pressed login button");

                    WaitForEvent<LoadEventFiredEvent>(chromeSession);

                    Console.WriteLine("And should be logged in now.");

                    var cookiesResponse = chromeSession.SendAsync(new GetCookiesCommand()).Result;
                    if (null != cookiesResponse.Result)
                    {
                        foreach (var cookie in cookiesResponse.Result.Cookies)
                        {
                            Console.WriteLine(cookie.Name + " : " + cookie.Value);
                        }
                    }


                    Console.WriteLine("Press enter to exit");
                    Console.ReadLine();
                }
            }).Wait();
        }

        private static async Task<string> ExecuteJavascript(IChromeSession chromeSession, string javaScriptToExecute)
        {
            var evalResponse = await chromeSession.SendAsync(new EvaluateCommand
            {
                Expression = javaScriptToExecute,
            });
            if (evalResponse.Result.ExceptionDetails != null)
            {
                return evalResponse.Result.ExceptionDetails.ToString();
            }
            return evalResponse.Result.Result.Value == null ? "" : evalResponse.Result.Result.Value.ToString();
        }

        private static async Task TakeScreenShot(IChromeSession chromeSession, string output)
        {

            var documentNodeId = (await chromeSession.SendAsync(new GetDocumentCommand())).Result.Root.NodeId;
            var bodyNodeId =
                (await chromeSession.SendAsync(new QuerySelectorCommand
                {
                    NodeId = documentNodeId,
                    Selector = "body"
                })).Result.NodeId;
            var height = (await chromeSession.SendAsync(new GetBoxModelCommand { NodeId = bodyNodeId })).Result.Model.Height;

            await chromeSession.SendAsync(new SetDeviceMetricsOverrideCommand
            {
                Width = 1440,
                Height = height,
                Scale = 1
            });

            Console.WriteLine("Taking screenShot");
            var screenShot = await chromeSession.SendAsync(new CaptureScreenshotCommand { Format = "png" });

            var data = Convert.FromBase64String(screenShot.Result.Data);
            File.WriteAllBytes(output, data);
        }

        private static void WaitForEvent<T>(IChromeSession chromeSession) where T : class
        {
            var waiter = new ManualResetEventSlim();
            chromeSession.Subscribe<T>(eventFired =>
            {
                waiter.Set();
            });
            waiter.Wait();
        }
    }
}