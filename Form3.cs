using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApp6
{
    public partial class Form3 : Form
    {
        private List<string[]> _tokens;

        private Dictionary<string, string> _memory = new Dictionary<string, string>();
        private Dictionary<string, string> _typeTable = new Dictionary<string, string>();

        private RichTextBox rtbMemory;
        private Button btnRun;
        private Label lblTitle;

        public Form3(List<string[]> tokens)
        {
            _tokens = tokens;
            InitializeComponent();
            BuildUI();
        }

        private void Form3_Load(object sender, EventArgs e) { }

        // ── UI ───────────────────────────────────────────────────────────────
        private void BuildUI()
        {
            this.Text = "Phase 3 – Memory";
            this.Size = new Size(500, 420);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(30, 30, 30);

            lblTitle = new Label
            {
                Text = "Memory Output",
                Location = new Point(20, 15),
                AutoSize = true,
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.SteelBlue
            };
            this.Controls.Add(lblTitle);

            btnRun = new Button
            {
                Text = "▶  Run",
                Location = new Point(360, 10),
                Size = new Size(100, 32),
                BackColor = Color.SteelBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            btnRun.FlatAppearance.BorderSize = 0;
            btnRun.Click += BtnRun_Click;
            this.Controls.Add(btnRun);

            rtbMemory = new RichTextBox
            {
                Location = new Point(20, 55),
                Size = new Size(440, 300),
                ReadOnly = true,
                BackColor = Color.FromArgb(20, 20, 20),
                ForeColor = Color.LimeGreen,
                Font = new Font("Consolas", 11f),
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(rtbMemory);
        }

        // ── Run ──────────────────────────────────────────────────────────────
        private void BtnRun_Click(object sender, EventArgs e)
        {
            rtbMemory.Clear();
            _memory.Clear();
            _typeTable.Clear();

            if (_tokens == null || _tokens.Count == 0)
            {
                Append("No tokens to execute.", Color.Gray);
                return;
            }

            var statements = SplitIntoStatements(_tokens);
            foreach (var stmt in statements)
                ExecuteStatement(stmt);

            Append("", Color.White);
            Append("── Memory ──────────────────", Color.DimGray);
            if (_memory.Count == 0)
            {
                Append("(empty)", Color.Gray);
            }
            else
            {
                foreach (var kv in _memory)
                    Append($"{kv.Key} = {kv.Value}", Color.LimeGreen);
            }
        }

        // ── Statement splitting ──────────────────────────────────────────────
        private List<List<string[]>> SplitIntoStatements(List<string[]> tokens)
        {
            var statements = new List<List<string[]>>();
            var current = new List<string[]>();
            bool insideBlock = false;

            foreach (var tok in tokens)
            {
                current.Add(tok);

                if (tok[0] == "for" || tok[0] == "while" || tok[0] == "if")
                    insideBlock = true;

                if (insideBlock && tok[1] == "Close Curly Bracket")
                {
                    statements.Add(current);
                    current = new List<string[]>();
                    insideBlock = false;
                    continue;
                }

                if (!insideBlock && tok[1] == "Semicolon")
                {
                    statements.Add(current);
                    current = new List<string[]>();
                }
            }

            if (current.Count > 0)
                statements.Add(current);

            return statements;
        }

        // ── Dispatch ─────────────────────────────────────────────────────────
        private void ExecuteStatement(List<string[]> toks)
        {
            if (toks.Count == 0) return;

            string firstType = toks[0][1];
            string first = toks[0][0];

            if (firstType == "Identifier") { ExecuteDeclaration(toks); return; }
            if (firstType == "Variable") { ExecuteReassignment(toks); return; }
            if (first == "for") { ExecuteForLoop(toks); return; }
            if (first == "while") { ExecuteWhileLoop(toks); return; }
            if (first == "if") { ExecuteIfStatement(toks); return; }
        }

        // ── Declaration  e.g.  int x = 5;  or  string s = "hi"; ─────────────
        private void ExecuteDeclaration(List<string[]> toks)
        {
            int pos = 0;
            string type = toks[pos][0]; pos++;   // e.g. "int", "string"

            if (pos >= toks.Count || toks[pos][1] != "Variable") return;
            string varName = toks[pos][0]; pos++;

            _typeTable[varName] = type;

            // Declaration without initialiser  →  default value
            if (pos >= toks.Count || toks[pos][1] == "Semicolon")
            {
                _memory[varName] = DefaultValue(type);
                PrintMemory(varName);
                return;
            }

            if (toks[pos][1] != "Equal") return;
            pos++;

            string value = EvalExpression(toks, ref pos, type);
            _memory[varName] = value;
            PrintMemory(varName);
        }

        // ── Reassignment  e.g.  x = x + 1; ──────────────────────────────────
        private void ExecuteReassignment(List<string[]> toks)
        {
            int pos = 0;
            string varName = toks[pos][0]; pos++;

            if (!_memory.ContainsKey(varName)) return;
            if (pos >= toks.Count || toks[pos][1] != "Equal") return;
            pos++;

            string declaredType = _typeTable.ContainsKey(varName) ? _typeTable[varName] : "auto";
            string value = EvalExpression(toks, ref pos, declaredType);
            _memory[varName] = value;
            PrintMemory(varName);
        }

        // ── For loop ─────────────────────────────────────────────────────────
        private void ExecuteForLoop(List<string[]> toks)
        {
            int pos = 0;
            Expect(toks, ref pos, "for");
            Expect(toks, ref pos, "Open Bracket");

            var initToks = ReadUntilSemicolon(toks, ref pos);
            ExecuteStatement(initToks);

            var condToks = ReadUntilSemicolon(toks, ref pos);
            var updateToks = ReadUntilCloseBracket(toks, ref pos);
            var bodyToks = ReadBody(toks, ref pos);

            int guard = 0;
            while (EvalCondition(condToks) && guard++ < 10000)
            {
                foreach (var s in SplitIntoStatements(bodyToks))
                    ExecuteStatement(s);

                var updateWithSemi = new List<string[]>(updateToks);
                updateWithSemi.Add(new[] { ";", "Semicolon" });
                ExecuteStatement(updateWithSemi);
            }
        }

        // ── While loop ───────────────────────────────────────────────────────
        private void ExecuteWhileLoop(List<string[]> toks)
        {
            int pos = 0;
            Expect(toks, ref pos, "while");
            Expect(toks, ref pos, "Open Bracket");

            var condToks = ReadUntilCloseBracket(toks, ref pos);
            var bodyToks = ReadBody(toks, ref pos);

            int guard = 0;
            while (EvalCondition(condToks) && guard++ < 10000)
            {
                foreach (var s in SplitIntoStatements(bodyToks))
                    ExecuteStatement(s);
            }
        }

        // ── If statement ─────────────────────────────────────────────────────
        private void ExecuteIfStatement(List<string[]> toks)
        {
            int pos = 0;
            Expect(toks, ref pos, "if");
            Expect(toks, ref pos, "Open Bracket");

            var condToks = ReadUntilCloseBracket(toks, ref pos);
            var bodyToks = ReadBody(toks, ref pos);

            if (EvalCondition(condToks))
            {
                foreach (var s in SplitIntoStatements(bodyToks))
                    ExecuteStatement(s);
            }
        }

        // ── Expression evaluator (type-aware) ────────────────────────────────
        // hintType lets the evaluator know whether to treat the result as
        // a string, bool, char, or numeric value.
        private string EvalExpression(List<string[]> toks, ref int pos, string hintType = "auto")
        {
            string left = GetOperandRaw(toks, ref pos);

            while (pos < toks.Count && toks[pos][1] == "Operator")
            {
                string op = toks[pos][0]; pos++;
                string right = GetOperandRaw(toks, ref pos);

                // ── String / char concatenation with + ───────────────────────
                bool leftIsNum = double.TryParse(left, out double lv);
                bool rightIsNum = double.TryParse(right, out double rv);

                if (op == "+" && (!leftIsNum || !rightIsNum))
                {
                    left = left + right;
                    continue;
                }

                // ── Numeric arithmetic ────────────────────────────────────────
                double l = leftIsNum ? lv : 0;
                double r = rightIsNum ? rv : 0;
                double result = 0;

                switch (op)
                {
                    case "+": result = l + r; break;
                    case "-": result = l - r; break;
                    case "*": result = l * r; break;
                    case "/": result = r != 0 ? l / r : 0; break;
                    case "%": result = r != 0 ? l % r : 0; break;
                }

                left = (result == Math.Floor(result) && !double.IsInfinity(result))
                       ? ((long)result).ToString()
                       : result.ToString();
            }

            // ── Coerce to declared type ──────────────────────────────────────
            return CoerceValue(left, hintType);
        }

        // Returns the raw string value of the next token (number, literal, or variable)
        private string GetOperandRaw(List<string[]> toks, ref int pos)
        {
            if (pos >= toks.Count) return "0";

            string tok = toks[pos][0];
            string type = toks[pos][1];
            pos++;

            switch (type)
            {
                case "Number": return tok;
                case "StringLiteral": return tok;
                case "CharLiteral": return tok;
                case "BoolLiteral": return tok;           // "true" / "false"
                case "Variable":
                    return _memory.ContainsKey(tok) ? _memory[tok] : "0";
                default:
                    return "0";
            }
        }

        // GetOperand used by EvalCondition (numeric context)
        private double GetOperand(List<string[]> toks, ref int pos)
        {
            string raw = GetOperandRaw(toks, ref pos);
            if (double.TryParse(raw, out double v)) return v;
            if (raw == "true") return 1;
            if (raw == "false") return 0;
            return 0;
        }

        // ── Condition evaluator ───────────────────────────────────────────────
        private bool EvalCondition(List<string[]> toks)
        {
            if (toks.Count < 3) return false;

            int pos = 0;

            // Check for string/bool comparison
            string leftRaw = GetOperandRaw(toks, ref pos);
            if (pos >= toks.Count) return false;

            string op = toks[pos][0]; pos++;

            string rightRaw = GetOperandRaw(toks, ref pos);

            // Try numeric comparison first
            if (double.TryParse(leftRaw, out double l) && double.TryParse(rightRaw, out double r))
            {
                switch (op)
                {
                    case "<": return l < r;
                    case ">": return l > r;
                    case "=": return l == r;
                    case "<=": return l <= r;
                    case ">=": return l >= r;
                    case "!=": return l != r;
                }
            }

            // String / bool equality
            switch (op)
            {
                case "=": return string.Equals(leftRaw, rightRaw, StringComparison.Ordinal);
                case "!=": return !string.Equals(leftRaw, rightRaw, StringComparison.Ordinal);
            }

            return false;
        }

        // ── Type coercion ─────────────────────────────────────────────────────
        private string CoerceValue(string value, string type)
        {
            switch (type)
            {
                case "int":
                    if (double.TryParse(value, out double d))
                        return ((long)d).ToString();
                    return "0";

                case "float":
                case "double":
                    if (double.TryParse(value, out double fd))
                        return fd.ToString();
                    return "0";

                case "bool":
                    if (value == "true" || value == "false") return value;
                    if (double.TryParse(value, out double bd))
                        return bd != 0 ? "true" : "false";
                    return "false";

                case "char":
                    return value.Length > 0 ? value[0].ToString() : "";

                case "string":
                    return value;   // already a string

                default:            // "auto" or unknown – return as-is
                    return value;
            }
        }

        // ── Helper readers ────────────────────────────────────────────────────
        private List<string[]> ReadUntilSemicolon(List<string[]> toks, ref int pos)
        {
            var result = new List<string[]>();
            while (pos < toks.Count && toks[pos][1] != "Semicolon")
                result.Add(toks[pos++]);
            if (pos < toks.Count) pos++;   // skip ;
            return result;
        }

        private List<string[]> ReadUntilCloseBracket(List<string[]> toks, ref int pos)
        {
            var result = new List<string[]>();
            int depth = 0;
            while (pos < toks.Count)
            {
                if (toks[pos][1] == "Open Bracket") depth++;
                if (toks[pos][1] == "Close Bracket")
                {
                    if (depth == 0) { pos++; break; }
                    depth--;
                }
                result.Add(toks[pos++]);
            }
            return result;
        }

        private List<string[]> ReadBody(List<string[]> toks, ref int pos)
        {
            var result = new List<string[]>();
            if (pos < toks.Count && toks[pos][1] == "Open Curly Bracket") pos++;
            int depth = 0;
            while (pos < toks.Count)
            {
                if (toks[pos][1] == "Open Curly Bracket") depth++;
                if (toks[pos][1] == "Close Curly Bracket")
                {
                    if (depth == 0) { pos++; break; }
                    depth--;
                }
                result.Add(toks[pos++]);
            }
            return result;
        }

        private void Expect(List<string[]> toks, ref int pos, string value)
        {
            if (pos < toks.Count && toks[pos][0] == value) pos++;
        }

        // ── Output helpers ────────────────────────────────────────────────────
        private void PrintMemory(string changedVar)
        {
            Append($"{changedVar} = {_memory[changedVar]}", Color.LimeGreen);
        }

        private void Append(string text, Color color)
        {
            rtbMemory.SelectionStart = rtbMemory.TextLength;
            rtbMemory.SelectionLength = 0;
            rtbMemory.SelectionColor = color;
            rtbMemory.AppendText(text + Environment.NewLine);
            rtbMemory.SelectionColor = rtbMemory.ForeColor;
        }

        // ── Default values ────────────────────────────────────────────────────
        private string DefaultValue(string type)
        {
            switch (type)
            {
                case "int":
                case "float":
                case "double": return "0";
                case "bool": return "false";
                case "char":
                case "string": return "";
                default: return "0";
            }
        }
    }
}