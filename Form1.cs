using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApp6
{
    public partial class Form1 : Form
    {
        private static readonly HashSet<string> Identifiers = new HashSet<string>
        {
            "int", "float", "string", "double", "bool", "char"
        };

        private static readonly HashSet<string> ReservedWords = new HashSet<string>
        {
            "for", "while", "if", "do", "return", "break", "continue", "end"
        };

        private static readonly HashSet<string> Operators = new HashSet<string>
        {
            "+", "-", "/", "%", "*"
        };

        private static readonly Dictionary<string, string> Symbols = new Dictionary<string, string>
        {
            { "(", "Open Bracket" },
            { ")", "Close Bracket" },
            { "{", "Open Curly Bracket" },
            { "}", "Close Curly Bracket" },
            { ",", "Comma" },
            { ";", "Semicolon" },
            { "&&", "And" },
            { "||", "Or" },
            { "<", "Less than" },
            { ">", "Greater than" },
            { "=", "Equal" },
            { "'", "Single Quote" },
            { "\"", "Double Quote" },
            { "!", "Not" }
        };

        public Form1()
        {
            InitializeComponent();
            SetupDataGridView();
        }

        private void SetupDataGridView()
        {
            DataGridView dgv = new DataGridView();
            dgv.Name = "dgvTokens";
            dgv.Location = new Point(100, 130);
            dgv.Size = new Size(560, 300);
            dgv.ReadOnly = true;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.BackgroundColor = SystemColors.Control;
            dgv.BorderStyle = BorderStyle.Fixed3D;

            dgv.Columns.Add("colToken", "Token");
            dgv.Columns.Add("colType", "Type");

            this.Controls.Add(dgv);
        }

        private void Form1_Load(object sender, EventArgs e) { }

        private void textBox1_TextChanged(object sender, EventArgs e) { }

        private void button1_Click(object sender, EventArgs e)
        {
            string input = textBox1.Text.Trim();

            DataGridView dgv = (DataGridView)this.Controls["dgvTokens"];
            dgv.Rows.Clear();

            if (string.IsNullOrEmpty(input))
                return;

            List<string[]> tokens = Scan(input);

            foreach (var tok in tokens)
                dgv.Rows.Add(tok[0], tok[1]);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string input = textBox1.Text.Trim();

            if (string.IsNullOrEmpty(input))
            {
                MessageBox.Show("Please enter code in the text box first.", "No Input",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            List<string[]> tokens = Scan(input);
            Form2 f2 = new Form2(tokens);
            f2.Show();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string input = textBox1.Text.Trim();

            if (string.IsNullOrEmpty(input))
            {
                MessageBox.Show("Please enter code in the text box first.", "No Input",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            List<string[]> tokens = Scan(input);
            Form3 f3 = new Form3(tokens);
            f3.Show();
        }

        private List<string[]> Scan(string input)
        {
            var tokens = new List<string[]>();
            int i = 0;

            while (i < input.Length)
            {
                // Skip whitespace
                if (char.IsWhiteSpace(input[i]))
                {
                    i++;
                    continue;
                }

                // ── Quoted string literal  "..."  ────────────────────────────
                if (input[i] == '"')
                {
                    int start = ++i;                         // skip opening "
                    while (i < input.Length && input[i] != '"') i++;
                    string literal = input.Substring(start, i - start);
                    if (i < input.Length) i++;               // skip closing "
                    tokens.Add(new[] { literal, "StringLiteral" });
                    continue;
                }

                // ── Quoted char literal  '.'  ────────────────────────────────
                if (input[i] == '\'')
                {
                    int start = ++i;                         // skip opening '
                    while (i < input.Length && input[i] != '\'') i++;
                    string literal = input.Substring(start, i - start);
                    if (i < input.Length) i++;               // skip closing '
                    tokens.Add(new[] { literal, "CharLiteral" });
                    continue;
                }

                // ── Two-character symbols (&&, ||, <=, >=, !=) ──────────────
                if (i + 1 < input.Length)
                {
                    string two = input.Substring(i, 2);
                    if (Symbols.ContainsKey(two))
                    {
                        tokens.Add(new[] { two, Symbols[two] });
                        i += 2;
                        continue;
                    }
                }

                // ── Single-character symbols ─────────────────────────────────
                string one = input[i].ToString();
                if (Symbols.ContainsKey(one))
                {
                    tokens.Add(new[] { one, Symbols[one] });
                    i++;
                    continue;
                }

                // ── Operators ────────────────────────────────────────────────
                if (Operators.Contains(one))
                {
                    tokens.Add(new[] { one, "Operator" });
                    i++;
                    continue;
                }

                // ── Numeric literal ──────────────────────────────────────────
                if (char.IsDigit(input[i]) ||
                    (input[i] == '.' && i + 1 < input.Length && char.IsDigit(input[i + 1])))
                {
                    int start = i;
                    while (i < input.Length && (char.IsDigit(input[i]) || input[i] == '.'))
                        i++;
                    tokens.Add(new[] { input.Substring(start, i - start), "Number" });
                    continue;
                }

                // ── Keyword / identifier / variable ──────────────────────────
                if (char.IsLetter(input[i]) || input[i] == '_')
                {
                    int start = i;
                    while (i < input.Length && (char.IsLetterOrDigit(input[i]) || input[i] == '_'))
                        i++;
                    string word = input.Substring(start, i - start);

                    if (word == "true" || word == "false")
                        tokens.Add(new[] { word, "BoolLiteral" });
                    else if (Identifiers.Contains(word))
                        tokens.Add(new[] { word, "Identifier" });
                    else if (ReservedWords.Contains(word))
                        tokens.Add(new[] { word, "Reserved Word" });
                    else
                        tokens.Add(new[] { word, "Variable" });
                    continue;
                }

                // ── Unknown ──────────────────────────────────────────────────
                tokens.Add(new[] { one, "Unknown" });
                i++;
            }

            return tokens;
        }
    }
}