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
        public class Lexem
        {
            public Lexem(Lexem Parent, ref int lineNumber, string Line, int currentIndent, string[] lines)
            {
                this.lineNumber = lineNumber;
                this.Line       = Line;
                this.Parent     = Parent;
            }

            public readonly Lexem  Parent;
            public readonly int    lineNumber;
            public readonly string Line;

            /// <summary>Вершины, непосредственно достижимые из этой</summary>
            public readonly SortedList<string, Lexem>       childs    = new SortedList<string, Lexem>();

            /// <summary>Достижимые вершины</summary>
            public readonly SortedList<string, PathToLexem> reachable = new SortedList<string, PathToLexem>();

            public override string ToString()
            {
                return $"{Line}";
            }

            /// <summary>Шаг алгоритма вычисления достижимости вершин</summary>
            /// <returns>True, если обнаружены новые пути</returns>
            public virtual bool doReachableStep()
            {
                bool isAdded = false;
                foreach (var lexem in childs)
                {
                    // Вершина непосредственно достижима
                    addPath(ref isAdded, lexem.Key, lexem.Key, 1);

                    // Вершина достижима через другие вершины
                    foreach (var lr in lexem.Value.reachable)
                    {
                        // Добавляем соотв. вершину
                        // Стартовая вершина пути - это вершина просматриваемого потомка
                        // Конечная вершина такая же, берётся из спискадостижимости
                        addPath(ref isAdded, lexem.Key, lr.Key, lr.Value.PathLen + 1);
                    }
                }

                return isAdded;
            }

            /// <summary>Добавляет в список достижимых вершин конец нового пути и его длину</summary>
            /// <param name="isAdded">Если верниша добавлена, isAdded = true. Иначе остаётся неизменным</param>
            /// <param name="EndOfPathLexem">Вершина, являющаяся концом пути</param>
            /// <param name="PathLen">Длина пути</param>
            protected void addPath(ref bool isAdded, String StartOfPathLexem, string EndOfPathLexem, int PathLen)
            {
                if (!reachable.ContainsKey(EndOfPathLexem))
                {
                    isAdded = true;
                    reachable.Add(EndOfPathLexem, new PathToLexem(PathLen, StartOfPathLexem));
                }
                else
                {
                    var a = reachable[EndOfPathLexem];
                    if (a.PathLen > PathLen)
                    {
                        isAdded = true;
                        reachable[EndOfPathLexem] = new PathToLexem(PathLen, StartOfPathLexem);
                    }
                }
            }

            public virtual string Name => Line;

            public virtual void ReachabilityToString(StringBuilder sb, string indent)
            {
                sb.AppendLine(indent + Name);
                indent = "\t" + indent;
                foreach (var a in reachable)
                {
                    if (a.Key == "_")
                        continue;

                    if (a.Value.PathLen == 0)
                        sb.AppendLine($"{indent}{a.Key}: 0");
                    else
                    if (a.Value.PathLen == 1)
                        sb.AppendLine($"{indent}{a.Key}: 1");
                    else
                    if (a.Value.PathLen >= 2)
                    {
                        var a1 = this.childs[a.Value.StartPathToReachableLexem];
                        var a2 = a1  .childs[a1.reachable[Name].StartPathToReachableLexem];

                        sb.AppendLine($"{indent}{a.Key}: {a.Value.PathLen}, {a.Value.StartPathToReachableLexem}, {a2}");
                    }
                }
            }
        }

        public class PathToLexem
        {
            public PathToLexem(int PathLen, string ReachableLexem)
            {
                this.PathLen                   = PathLen;
                this.StartPathToReachableLexem = ReachableLexem;
            }

            public int    PathLen;
            public string StartPathToReachableLexem;

            public static bool operator >(PathToLexem a, PathToLexem b)
            {
                return a.PathLen > b.PathLen;
            }

            public static bool operator <(PathToLexem a, PathToLexem b)
            {
                return a.PathLen < b.PathLen;
            }
        }

        public class StateLexem: Lexem
        {
            public StateLexem(Lexem Parent, ref int lineNumber, string Line, int currentIndent, string[] lines): base(Parent, ref lineNumber, Line, currentIndent, lines)
            {
            }

            public virtual void Parse(ref int lineNumber, string Line, int currentIndent, string[] lines, bool fromStatesDeclaration = false, SortedList<string, Lexem> states = null)
            {
                if (states != null && !(this is DeclarationLexem))
                {
                    if (!states.ContainsKey(Line))
                        throw new Exception($"Unknown state at line {lineNumber+1}");
                }

                for (int i = lineNumber + 1; i < lines.Length; i++)
                {
                    lineNumber = i;

                    var curLine   = lines[i];
                    var lnTrimmed = lines[i].Trim();
                    if (lnTrimmed.StartsWith("#") || lnTrimmed.Length <= 0)
                        continue;

                    var childIndent = curLine.IndexOf(lnTrimmed); // curLine.Substring(startIndex: 0, curLine.IndexOf(lnTrimmed));
                    if (childIndent - 1 > currentIndent)
                        throw new Exception($"Incorrect indent at line {i+1} (see you have the same style of tabs or spaces and you have an only one symbol for a level)");

                    // Сейчас должно быть childIndent + 1 == currentIndent
                    if (childIndent <= currentIndent)
                    {
                        lineNumber = i - 1;
                        return;
                    }

                    if (fromStatesDeclaration && lnTrimmed == "_")
                        throw new Exception($"'_' symbol occured at line {i+1}, but '_' is a keyword");

                    StateLexem LineLexem = null;
                    if (fromStatesDeclaration)
                        LineLexem = new StateLexem(this, lineNumber: ref i, Line: lnTrimmed, currentIndent: childIndent, lines);
                    else
                        LineLexem = states[lnTrimmed] as StateLexem;

                    LineLexem.Parse(lineNumber: ref i, Line: lnTrimmed, currentIndent: childIndent, lines, fromStatesDeclaration: fromStatesDeclaration, states: states);

                    if (childs.ContainsKey(LineLexem.Line))
                        throw new Exception($"{LineLexem.Line} is declared in the top level of tree twice at line {i + 1}");

                    childs.Add(LineLexem.Line, LineLexem);
                }

                lineNumber = lines.Length;
            }
        }

        public class EmptyLexem: StateLexem
        {
            public EmptyLexem(Lexem Parent, int lineNumber): base(Parent, ref lineNumber, "_", 0, null)
            {
            }

            public override bool doReachableStep()
            {
                if (this.childs.Count > 0)
                    throw new Exception($"Empty state '_' can not have childs {childs.Keys[0]}");

                return false;
            }
        }

        public class DeclarationLexem: StateLexem
        {
            public readonly string Declaration;
            public readonly string NameOfDeclaration;

            public override string Name => NameOfDeclaration;

            public DeclarationLexem(ref int lineNumber, string Line, int currentIndent, string[] lines, FiniteMachineChecker fmc): base(null, ref lineNumber, Line, currentIndent, lines)
            {
                var a = Line.Split(new string[] {":"}, StringSplitOptions.None);

                if (a.Length != 2)
                    throw new Exception($"Incorrect declaration at line {lineNumber + 1}");

                Declaration       = a[0].Trim();
                NameOfDeclaration = a[1].Trim();

                if (Declaration == "STATES")
                {
                    // Добавляем служебную лексему "_"
                    this.childs.Add("_", new EmptyLexem(this, 0));

                    Parse(lineNumber: ref lineNumber, Line: Line, currentIndent: currentIndent, lines, fromStatesDeclaration: true, null);
                }
                else
                if (Declaration == "TRANS")
                {
                    Parse(lineNumber: ref lineNumber, Line: Line, currentIndent: currentIndent, lines, fromStatesDeclaration: false, fmc.StatesLexems[NameOfDeclaration].childs);

                    foreach (var node in fmc.StatesLexems[NameOfDeclaration].childs)
                    {
                        if (node.Value is EmptyLexem)
                            continue;

                        if (!this.childs.ContainsKey(node.Key))
                            throw new Exception($"Not enought declaration in transitions: {node.Value}");
                    }
                }
            }

            public override void ReachabilityToString(StringBuilder sb, string indent)
            {
                sb.AppendLine(indent + Name);
                indent = "\t" + indent;
                foreach (var a in childs)
                {
                    if (a.Value is EmptyLexem)
                        continue;

                    a.Value.ReachabilityToString(sb, indent);
                }
            }

            public override bool doReachableStep()
            {
                bool isAdded = false;
                foreach (var lexem in childs)
                {
                    isAdded |= lexem.Value.doReachableStep();
                }

                return isAdded;
            }
        }
    }
}
