using System;
using System.Collections.Generic;
using System.IO;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;

namespace RemoteCSharpShell
{
    public class VirtualTerminal
    {
        private readonly TextReader @in;
        private readonly TextWriter @out;
        private readonly List<string> history;

        private ConsoleColor foregroundColor;

        public ConsoleColor ForegroundColor
        {
            get { return this.foregroundColor; }
            set
            {
                if (value != this.foregroundColor)
                {
                    this.@out.Write("\x1b[{0}m", ColorToAnsi(value));
                    this.foregroundColor = value;
                }
            }
        }

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
                WriteCode(line);
                this.@out.Write("\x1b[{0}G", prompt.Length + (char)inTextPosition + 1);
            }
        }

        public void WriteCode(string line)
        {
            var options = new ParseOptions(
                CompatibilityMode.None,
                LanguageVersion.CSharp4,
                true,
                SourceCodeKind.Interactive,
                default(ReadOnlyArray<string>));


            var tree = SyntaxTree.ParseText(line, options: options);

            var root = tree.GetRoot();

            foreach (var item in root.DescendantTokens())
            {
                this.ForegroundColor = ConsoleColor.White;

                this.@out.Write(item.LeadingTrivia);

                if (item.IsKeyword() || item.IsReservedKeyword())
                {
                    this.ForegroundColor = ConsoleColor.Yellow;
                    this.@out.Write(item);
                }
                else if (item.Kind == SyntaxKind.StringLiteralToken)
                {
                    this.ForegroundColor = ConsoleColor.Green;
                    this.@out.Write(item);
                }
                else if (item.Kind == SyntaxKind.NumericLiteralToken)
                {
                    this.ForegroundColor = ConsoleColor.Green;
                    this.@out.Write(item);
                }
                else
                {
                    this.@out.Write(item);
                }

                this.@out.Write(item.TrailingTrivia);
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

        public void WriteLine()
        {
            this.@out.WriteLine();
        }

        public void WriteLine(string value)
        {
            this.@out.WriteLine(value);
        }

        public static string ColorToAnsi(ConsoleColor value)
        {
            switch (value)
            {
                case ConsoleColor.Black: return "0;30";
                case ConsoleColor.White: return "1;37";

                case ConsoleColor.DarkBlue: return "0;34";
                case ConsoleColor.DarkGreen: return "0;32";
                case ConsoleColor.DarkCyan: return "0;36";
                case ConsoleColor.DarkRed: return "0;31";
                case ConsoleColor.DarkMagenta: return "0;35";
                case ConsoleColor.DarkYellow: return "0;33";
                case ConsoleColor.DarkGray: return "0;33";

                case ConsoleColor.Blue: return "1;34";
                case ConsoleColor.Green: return "1;32";
                case ConsoleColor.Cyan: return "1;36";
                case ConsoleColor.Red: return "1;31";
                case ConsoleColor.Magenta: return "1;35";
                case ConsoleColor.Yellow: return "1;33";
                case ConsoleColor.Gray: return "1;37";
                default:
                    throw new ArgumentOutOfRangeException("value");
            }
        }
    }
}