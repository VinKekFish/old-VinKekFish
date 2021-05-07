using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FiniteMachineChecker
{
    public partial class FiniteMachineChecker
    {
        public          string[] lines;
        public readonly string   FileName = null;

        public readonly SortedList<string, Lexem> StatesLexems      = new SortedList<string, Lexem>(128);

        public readonly SortedList<string, Lexem> TransitionsLexems = new SortedList<string, Lexem>(128);

        public FiniteMachineChecker(string FileName): this(File.ReadAllLines(FileName))
        {
            this.FileName = FileName;
        }

        /// <summary>Парсит строки описания графа состояний</summary>
        /// <param name="lines">Строки файла</param>
        /// <remarks>Допускаются пустые строки. Допускаются однострочные комментарии: строка начинается с символа "#", не считая пробелов и табуляций</remarks>
        public FiniteMachineChecker(string[] lines)
        {
            this.lines = lines;

            var currentIndent = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                var curLine   = lines[i];
                var lnTrimmed = lines[i].Trim();
                if (lnTrimmed.StartsWith("#") || lnTrimmed.Length <= 0)
                    continue;

                if (curLine.StartsWith(" ") || curLine.StartsWith("\t"))
                    throw new Exception($"Incorrect indent at line {i+1} or above (last object ended but the new line has indent)");

                var LineLexem = new DeclarationLexem(lineNumber: ref i, Line: lnTrimmed, currentIndent: currentIndent, lines, this);

                switch (LineLexem.Declaration)
                {
                    case "STATES":
                                    StatesLexems.Add(LineLexem.Name, LineLexem);
                                    break;
                    case "TRANS":
                                    TransitionsLexems.Add(LineLexem.Name, LineLexem);
                                    break;
                    default:
                                    throw new Exception($"Unknown declaration type at line {i + 1}");
                }
            }

            calcReachability();
        }

        public string ReachabilityToString()
        {
            var sb = new StringBuilder(1024);
            foreach (var lexem in TransitionsLexems)
            {
                var decl = lexem.Value as DeclarationLexem;
                sb.AppendLine("Достижимость");
                decl.ReachabilityToString(sb, "\t");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        public void calcReachability()
        {
            bool IsAdded = false;
            do
            {
                IsAdded = false;

                foreach (var lexem in TransitionsLexems)
                {
                    IsAdded |= lexem.Value.doReachableStep();
                }
            }
            while (IsAdded);
        }
    }
}
