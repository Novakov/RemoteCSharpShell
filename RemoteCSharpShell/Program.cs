using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ScriptCs;
using ScriptCs.Contracts;
using ScriptCs.Hosting;
using ScriptCs.Engine.Roslyn;

namespace RemoteCSharpShell
{
    class Program
    {
        static void Main(string[] args)
        {
            var replServer = new TcpListener(IPAddress.Loopback, 1234);
            replServer.Start();
            Console.WriteLine("Waiting for connection");

            var client = replServer.AcceptTcpClient();
            Console.WriteLine("Accepted connection {0}", client.Client.RemoteEndPoint);

            var stream = client.GetStream();

            using (var reader = new StreamReader(stream))
            {
                using (var writer = new StreamWriter(stream))
                {
                    writer.AutoFlush = true;

                    RunRepl(reader, writer);
                }
            }

            client.Close();

            replServer.Stop();
        }

        private static void RunRepl(TextReader input, TextWriter output)
        {
            var vt = new VirtualTerminal(input, output);

            var console = new InOutConsole(vt);

            var scriptServices = BuildScriptServices(console);         

            while (true)
            {
                string line = vt.ReadLine(">");

                if (line == "q")
                {
                    scriptServices.Repl.Terminate();
                    break;
                }                

                if (!string.IsNullOrWhiteSpace(line))
                {
                    vt.RecordHistoryLine(line);
                }
                
                scriptServices.Repl.Execute(line);
            }
        }

        private static ScriptServices BuildScriptServices(InOutConsole console)
        {
            var logConfiguration = new LoggerConfigurator(LogLevel.Info);
            logConfiguration.Configure(console);
            var logger = logConfiguration.GetLogger();

            var initializationServices = new InitializationServices(logger);

            initializationServices.GetAppDomainAssemblyResolver().Initialize();

            var scriptServicesBuilder = new ScriptServicesBuilder(console, logger, null, null, initializationServices)
                .Repl(true);

            scriptServicesBuilder.LoadModules("");

            var scriptServices = scriptServicesBuilder.Build();

            initializationServices.GetInstallationProvider().Initialize();

            scriptServices.Repl.Initialize(Enumerable.Empty<string>(), Enumerable.Empty<IScriptPack>());
            return scriptServices;
        }
    }
}
