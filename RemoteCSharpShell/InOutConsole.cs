using System;
using System.IO;
using ScriptCs.Contracts;

namespace RemoteCSharpShell
{
    public class InOutConsole : IConsole
    {
        private readonly TextReader @in;
        private readonly TextWriter @out;
        private ConsoleColor foregroundColor;

        public InOutConsole(TextReader @in, TextWriter @out)
        {
            this.@in = @in;
            this.@out = @out;
        }

        public void Write(string value)
        {
            this.@out.Write(value);
        }

        public void WriteLine()
        {
            this.@out.WriteLine();
        }

        public void WriteLine(string value)
        {
            this.@out.WriteLine(value);
        }

        public string ReadLine()
        {
            return this.@in.ReadLine();
        }

        public void Clear()
        {

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
            get { return this.foregroundColor; }
            set
            {
                this.@out.Write("\x1b[{0}m", ColorToAnsi(value));
                this.foregroundColor = value;
            }
        }

        public static string ColorToAnsi(ConsoleColor value)
        {
            switch (value)
            {
                case ConsoleColor.Black:        return "0;30";
                case ConsoleColor.White:        return "1;37";

                case ConsoleColor.DarkBlue:     return "0;34";
                case ConsoleColor.DarkGreen:    return "0;32";
                case ConsoleColor.DarkCyan:     return "0;36";
                case ConsoleColor.DarkRed:      return "0;31";
                case ConsoleColor.DarkMagenta:  return "0;35";
                case ConsoleColor.DarkYellow:   return "0;33";
                case ConsoleColor.DarkGray:     return "0;33";

                case ConsoleColor.Blue:         return "1;34";
                case ConsoleColor.Green:        return "1;32";
                case ConsoleColor.Cyan:         return "1;36";
                case ConsoleColor.Red:          return "1;31";
                case ConsoleColor.Magenta:      return "1;35";
                case ConsoleColor.Yellow:       return "1;33";
                case ConsoleColor.Gray:         return "1;37";
                default:
                    throw new ArgumentOutOfRangeException("value");
            }
        }
    }
}