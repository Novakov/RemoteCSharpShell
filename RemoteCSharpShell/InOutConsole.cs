using System;
using ScriptCs.Contracts;

namespace RemoteCSharpShell
{
    public class InOutConsole : IConsole
    {
        private readonly VirtualTerminal terminal;

        public InOutConsole(VirtualTerminal terminal)
        {
            this.terminal = terminal;
        }

        public void Write(string value)
        {
            this.terminal.Write(value);
        }

        public void WriteLine()
        {
            this.terminal.WriteLine();
        }

        public void WriteLine(string value)
        {
            this.terminal.WriteLine(value);
        }

        public string ReadLine()
        {
            throw new NotImplementedException("ReadLine not implemented");
        }

        public void Clear()
        {
            throw new NotImplementedException("Clear not implemented");
        }

        public void Exit()
        {
            
        }

        public void ResetColor()
        {
            this.ForegroundColor = ConsoleColor.White;
        }

        public ConsoleColor ForegroundColor
        {
            get { return this.terminal.ForegroundColor; }
            set { this.terminal.ForegroundColor = value; }
        }       
    }
}