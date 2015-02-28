using System;
using System.Collections.Generic;
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
            var console = new InOutConsole(input, output);

            var scriptServices = BuildScriptServices(console);

            var vt = new VirtualTerminal(input, output);

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

    public class VirtualTerminal
    {
        private readonly TextReader @in;
        private readonly TextWriter @out;
        private readonly List<string> history;

        public VirtualTerminal(TextReader @in, TextWriter @out)
        {
            this.@in = @in;
            this.@out = @out;
            this.history = new List<string>();
        }

        public string ReadLine(string prompt)
        {
            string line = "";

            int inTextPosition = 0;
            int selectedHistoryEntry = 0;

            this.@out.Write(prompt);

            while (true)
            {
                var p = this.@in.Read();          

                switch ((ConsoleKey)p)
                {
                    case ConsoleKey.Escape:
                        switch (HandleEscape())
                        {
                            case EscapeCode.MoveLeft:
                                inTextPosition--;
                                break;
                            case EscapeCode.MoveRight:
                                inTextPosition++;
                                break;
                            case EscapeCode.Up:
                                selectedHistoryEntry = Bound(1, selectedHistoryEntry + 1, this.history.Count);
                                line = this.history[this.history.Count - selectedHistoryEntry];
                                break;
                            case EscapeCode.Down:
                                selectedHistoryEntry = Bound(1, selectedHistoryEntry - 1, this.history.Count);
                                line = this.history[this.history.Count - selectedHistoryEntry];
                                break;                           
                        }
                        break;
                    case ConsoleKey.Enter:
                        NewLine();
                        return line;
                    case ConsoleKey.F16: //backspace
                        if (inTextPosition > 0)
                        {
                            line = line.Remove(inTextPosition - 1, 1);
                            inTextPosition--;
                        }
                        break;
                    default:
                        line = line.Insert(inTextPosition, ((char)p).ToString());
                        inTextPosition++;
                        break;
                }

                inTextPosition = Bound(0, inTextPosition, line.Length);

                this.@out.Write("\x1b[s\x1b[0G\x1b[K");
                this.@out.Write(prompt);
                this.@out.Write(line);
                this.@out.Write("\x1b[{0}G", prompt.Length + (char)inTextPosition + 1);
            }
        }

        private int Bound(int lower, int value, int upper)
        {
            return Math.Max(lower, Math.Min(value, upper));
        }

        private void NewLine()
        {
            this.@out.WriteLine();
        }

        private EscapeCode HandleEscape()
        {
            var secondChar = this.@in.Read();

            if (secondChar == '[')
            {
                var actionCode = this.@in.Read();

                switch ((ConsoleKey)actionCode)
                {
                    case ConsoleKey.D:
                        return EscapeCode.MoveLeft;
                    case ConsoleKey.C:
                        return EscapeCode.MoveRight;
                    case ConsoleKey.A:
                        return EscapeCode.Up;
                    case ConsoleKey.B:
                        return EscapeCode.Down;                
                    default:
                        return EscapeCode.Unknown;
                }
            }

            return EscapeCode.Unknown;
        }

        public void Write(string text)
        {
            this.@out.Write(text);
        }

        public void RecordHistoryLine(string line)
        {
            this.history.Add(line);
        }

        private enum EscapeCode
        {
            MoveLeft,
            MoveRight,
            Unknown,
            Up,
            Down,           
        }
    }
}
