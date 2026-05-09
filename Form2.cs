using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApp6
{
    public partial class Form2 : Form
    {
        private List<string[]> _tokens;
        private Dictionary<string, string> _symbolTable = new Dictionary<string, string>();

        private DataGridView dgvTokens;
        private RichTextBox rtbResult;
        private Button btnAnalyze;
        private Label lblTokens, lblResult;


        public Form2(List<string[]> tokens)
        {
            _tokens = tokens;
            InitializeComponent();
            BuildUI();
            PopulateTokenGrid();
        }

        private void Form2_Load(object sender, EventArgs e) { }

        private void BuildUI()
        {
            this.Text = "Phase 2 – Semantic Analyzer";
            this.Size = new Size(720, 620);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = SystemColors.Control;

            lblTokens = new Label
            {
                Text = "Tokens from Phase 1:",
                Location = new Point(20, 10),
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            this.Controls.Add(lblTokens);

            dgvTokens = new DataGridView
            {
                Location = new Point(20, 32),
                Size = new Size(660, 160),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                BackgroundColor = Color.White
            };
            dgvTokens.Columns.Add("colToken", "Token");
            dgvTokens.Columns.Add("colType", "Type");
            this.Controls.Add(dgvTokens);

            btnAnalyze = new Button
            {
                Text = "Analyze",
                Location = new Point(20, 205),
                Size = new Size(100, 30),
                BackColor = Color.SteelBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnAnalyze.Click += BtnAnalyze_Click;
            this.Controls.Add(btnAnalyze);

            lblResult = new Label
            {
                Text = "Semantic Analysis Result:",
                Location = new Point(20, 248),
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            this.Controls.Add(lblResult);

            rtbResult = new RichTextBox
            {
                Location = new Point(20, 270),
                Size = new Size(660, 290),
                ReadOnly = true,
                BackColor = Color.White,
                Font = new Font("Consolas", 9.5f)
            };
            this.Controls.Add(rtbResult);
        }

        private void PopulateTokenGrid()
        {
            foreach (var tok in _tokens)
                dgvTokens.Rows.Add(tok[0], tok[1]);
        }

        private void BtnAnalyze_Click(object sender, EventArgs e)
        {
            rtbResult.Clear();
            _symbolTable.Clear();

            if (_tokens == null || _tokens.Count == 0)
            {
                AppendLine("No tokens to analyze.", Color.Gray);
                return;
            }

            var statements = SplitIntoStatements(_tokens);

            if (statements.Count == 0)
            {
                AppendLine("No statements found.", Color.OrangeRed);
                return;
            }

            bool allOk = true;
            for (int s = 0; s < statements.Count; s++)
            {
                List<string> errors = new List<string>();
                ValidateStatement(statements[s], errors);

                if (errors.Count == 0)
                {
                    AppendLine($"Statement {s + 1}: Valid", Color.Green);
                }
                else
                {
                    AppendLine($"Statement {s + 1}: Errors Found:", Color.Red);
                    foreach (var err in errors)
                        AppendLine($"        → {err}", Color.Red);
                    allOk = false;
                }
            }

            AppendLine("", Color.Black);
            if (allOk)
                AppendLine(" All statements are semantically correct.", Color.DarkGreen);
            else
                AppendLine(" Semantic errors detected.", Color.DarkRed);
        }
        private void ValidateStatement(List<string[]> toks, List<string> errors)
        {
            if (toks.Count == 0) return;
            if (toks[0][1] == "Reserved Word" && toks[0][0] == "for")
            {
                ValidateForLoop(toks, errors);
                return;
            }

            if (toks[0][1] == "Reserved Word" && toks[0][0] == "while")
            {
                ValidateWhileLoop(toks, errors);
                return;
            }

            if (toks[0][1] == "Reserved Word" && toks[0][0] == "if")
            {
                ValidateIfStatement(toks, errors);
                return;
            }

            if (toks[0][1] == "Identifier")
            {
                ValidateDeclaration(toks, errors);
                return;
            }

            if (toks[0][1] == "Variable")
            {
                ValidateReassignment(toks, errors);
                return;
            }

            errors.Add("Statement must start with a type identifier, declared variable, 'for', 'while', or 'if'");
        }

        private void ValidateForLoop(List<string[]> toks, List<string> errors)
        {
            int pos = 0;

            if (pos >= toks.Count || toks[pos][0] != "for")
            { errors.Add("Expected 'for' keyword"); return; }
            pos++;

            if (pos < toks.Count && toks[pos][1] == "Open Bracket")
                pos++;
            else
                errors.Add("Missing '(' after 'for'");

            if (pos < toks.Count && toks[pos][1] == "Close Bracket")
            {
                errors.Add("for loop is empty: missing initializer, condition, and update");
                pos++;

                if (pos < toks.Count && toks[pos][1] == "Open Curly Bracket")
                    pos++;
                else
                    errors.Add("Missing '{' to open for loop body");

                if (pos < toks.Count && toks[pos][1] == "Close Curly Bracket")
                    pos++;
                else
                    errors.Add("Missing '}' to close for loop body");

                return;
            }

            string loopVarName = null;
            string loopVarType = null;

            if (pos < toks.Count && toks[pos][1] == "Identifier")
            {
                loopVarType = toks[pos][0];
                pos++;

                if (pos < toks.Count && toks[pos][1] == "Variable")
                {
                    loopVarName = toks[pos][0];

                    if (_symbolTable.ContainsKey(loopVarName))
                        errors.Add($"Variable '{loopVarName}' has already been declared");

                    pos++;

                    if (pos < toks.Count && toks[pos][1] == "Equal")
                    {
                        pos++;

                        if (pos < toks.Count)
                        {
                            var rhsErrors = new List<string>();
                            ValidateRHSInPlace(toks, ref pos, loopVarType, rhsErrors);
                            errors.AddRange(rhsErrors);

                            if (rhsErrors.Count > 0)
                            {
                                while (pos < toks.Count && toks[pos][1] != "Semicolon")
                                    pos++;
                            }
                        }
                        else
                        {
                            errors.Add("Expected initial value after '=' in for loop");
                        }
                    }
                    else
                    {
                        errors.Add("Expected '=' in for loop initializer");
                    }

                    if (loopVarName != null && !_symbolTable.ContainsKey(loopVarName))
                        _symbolTable[loopVarName] = loopVarType;
                }
                else
                {
                    errors.Add("Expected variable name after type in for loop initializer");
                }
            }
            else
            {
                errors.Add("Expected type identifier (int, float, …) to start for loop initializer");
                while (pos < toks.Count && toks[pos][1] != "Semicolon")
                    pos++;
            }

            if (pos < toks.Count && toks[pos][1] == "Semicolon")
                pos++;
            else
                errors.Add("Missing ';' after for loop initializer");

            if (pos < toks.Count && toks[pos][1] == "Variable")
            {
                string condVar = toks[pos][0];
                if (!_symbolTable.ContainsKey(condVar))
                    errors.Add($"Variable '{condVar}' in condition is not declared");
                pos++;

                if (pos < toks.Count && IsComparator(toks[pos]))
                    pos++;
                else
                    errors.Add("Expected a comparison operator (<, >, =) in for loop condition");

                if (pos < toks.Count && IsVarOrNum(toks[pos]))
                {
                    if (toks[pos][1] == "Variable" && !_symbolTable.ContainsKey(toks[pos][0]))
                        errors.Add($"Variable '{toks[pos][0]}' in condition is not declared");
                    pos++;
                }
                else
                {
                    errors.Add("Expected a variable or number on right side of condition");
                }
            }
            else
            {
                errors.Add("Expected a variable to start the for loop condition");
                while (pos < toks.Count && toks[pos][1] != "Semicolon")
                    pos++;
            }

            if (pos < toks.Count && toks[pos][1] == "Semicolon")
                pos++;
            else
                errors.Add("Missing ';' after for loop condition");

            if (pos < toks.Count && toks[pos][1] == "Variable")
            {
                string updateVar = toks[pos][0];
                if (!_symbolTable.ContainsKey(updateVar))
                    errors.Add($"Variable '{updateVar}' in update is not declared");
                pos++;

                bool foundIncDec = false;

                if (pos < toks.Count && (toks[pos][0] == "++" || toks[pos][0] == "--"))
                {
                    foundIncDec = true;
                    pos++;
                }
                else if (pos + 1 < toks.Count
                         && toks[pos][1] == "Operator"
                         && toks[pos + 1][1] == "Operator"
                         && toks[pos][0] == toks[pos + 1][0]
                         && (toks[pos][0] == "+" || toks[pos][0] == "-"))
                {
                    foundIncDec = true;
                    pos += 2;
                }

                if (!foundIncDec)
                    errors.Add($"Expected '++' or '--' after '{updateVar}' in for loop update");
            }
            else
            {
                errors.Add("Expected a variable in the for loop update part");
                while (pos < toks.Count && toks[pos][1] != "Close Bracket")
                    pos++;
            }

            if (pos < toks.Count && toks[pos][1] == "Close Bracket")
                pos++;
            else
                errors.Add("Missing ')' after for loop update");

            if (pos < toks.Count && toks[pos][1] == "Open Curly Bracket")
                pos++;
            else
                errors.Add("Missing '{' to open for loop body");

            if (pos < toks.Count && toks[pos][1] == "Close Curly Bracket")
                pos++;
            else
                errors.Add("Missing '}' to close for loop body");

            if (pos < toks.Count)
                errors.Add($"Unexpected token '{toks[pos][0]}' after for loop");
        }

        private void ValidateWhileLoop(List<string[]> toks, List<string> errors)
        {
            int pos = 0;

            if (pos >= toks.Count || toks[pos][0] != "while")
            { errors.Add("Expected 'while' keyword"); return; }
            pos++;

            if (pos < toks.Count && toks[pos][1] == "Open Bracket")
                pos++;
            else
                errors.Add("Missing '(' after 'while'");

            ValidateCondition(toks, ref pos, "while", errors);

            if (pos < toks.Count && toks[pos][1] == "Close Bracket")
                pos++;
            else
                errors.Add("Missing ')' after while condition");

            if (pos < toks.Count && toks[pos][1] == "Open Curly Bracket")
                pos++;
            else
                errors.Add("Missing '{' to open while body");

            if (pos < toks.Count && toks[pos][1] == "Close Curly Bracket")
                pos++;
            else
                errors.Add("Missing '}' to close while body");

            if (pos < toks.Count)
                errors.Add($"Unexpected token '{toks[pos][0]}' after while loop");
        }

        private void ValidateIfStatement(List<string[]> toks, List<string> errors)
        {
            int pos = 0;

            if (pos >= toks.Count || toks[pos][0] != "if")
            { errors.Add("Expected 'if' keyword"); return; }
            pos++;

            if (pos < toks.Count && toks[pos][1] == "Open Bracket")
                pos++;
            else
                errors.Add("Missing '(' after 'if'");

            ValidateCondition(toks, ref pos, "if", errors);

            if (pos < toks.Count && toks[pos][1] == "Close Bracket")
                pos++;
            else
                errors.Add("Missing ')' after if condition");

            if (pos < toks.Count && toks[pos][1] == "Open Curly Bracket")
                pos++;
            else
                errors.Add("Missing '{' to open if body");

            if (pos < toks.Count && toks[pos][1] == "Close Curly Bracket")
                pos++;
            else
                errors.Add("Missing '}' to close if body");

            if (pos < toks.Count)
                errors.Add($"Unexpected token '{toks[pos][0]}' after if statement");
        }

        private void ValidateCondition(List<string[]> toks, ref int pos, string keyword, List<string> errors)
        {
            if (pos >= toks.Count || toks[pos][1] == "Close Bracket")
            {
                errors.Add($"Empty condition in '{keyword}' statement");
                return;
            }

            if (pos < toks.Count && toks[pos][1] == "Variable")
            {
                string condVar = toks[pos][0];

                if (!_symbolTable.ContainsKey(condVar))
                    errors.Add($"Variable '{condVar}' in condition is not declared");

                pos++;

                if (pos >= toks.Count || toks[pos][1] == "Close Bracket")
                {
                    if (_symbolTable.ContainsKey(condVar) && _symbolTable[condVar] != "bool")
                        errors.Add($"Variable '{condVar}' is not bool; a comparator is required");
                    return;
                }
                if (IsComparator(toks[pos]))
                    pos++;
                else
                    errors.Add($"Expected a comparison operator (<, >, =) in {keyword} condition");

                if (pos < toks.Count && IsVarOrNum(toks[pos]))
                {
                    if (toks[pos][1] == "Variable" && !_symbolTable.ContainsKey(toks[pos][0]))
                        errors.Add($"Variable '{toks[pos][0]}' in condition is not declared");
                    pos++;
                }
                else
                {
                    errors.Add($"Expected a variable or number on right side of {keyword} condition");
                }
            }
            else
            {
                errors.Add($"Expected a variable to start the {keyword} condition");
                while (pos < toks.Count && toks[pos][1] != "Close Bracket")
                    pos++;
            }
        }

        private void ValidateDeclaration(List<string[]> toks, List<string> errors)
        {
            int pos = 0;
            string declaredType = toks[pos][0];
            pos++;

            if (pos >= toks.Count || toks[pos][1] != "Variable")
            { errors.Add("Expected a variable name after the type identifier"); return; }

            string varName = toks[pos][0];
            pos++;

            if (_symbolTable.ContainsKey(varName))
                errors.Add($"Variable '{varName}' has already been declared");

            if (pos < toks.Count && toks[pos][1] == "Semicolon")
            {
                pos++;
                if (!_symbolTable.ContainsKey(varName))
                    _symbolTable[varName] = declaredType;
                return;
            }

            if (pos >= toks.Count || toks[pos][1] != "Equal")
            { errors.Add("Expected '=' or ';' after variable name"); return; }
            pos++;

            if (pos >= toks.Count)
            { errors.Add("Expected a value after '='"); return; }

            ValidateRHSInPlace(toks, ref pos, declaredType, errors);

            if (pos >= toks.Count || toks[pos][1] != "Semicolon")
                errors.Add("Expected ';' at end of statement");
            else pos++;

            if (pos < toks.Count)
                errors.Add($"Unexpected token '{toks[pos][0]}' after semicolon");

            if (!_symbolTable.ContainsKey(varName))
                _symbolTable[varName] = declaredType;

        }

        private void ValidateReassignment(List<string[]> toks, List<string> errors)
        {
            int pos = 0;
            string varName = toks[pos][0];
            pos++;

            if (!_symbolTable.ContainsKey(varName))
            { errors.Add($"Variable '{varName}' is used before being declared"); return; }

            string declaredType = _symbolTable[varName];

            if (pos >= toks.Count || toks[pos][1] != "Equal")
            { errors.Add("Expected '=' after variable name"); return; }
            pos++;

            if (pos >= toks.Count)
            { errors.Add("Expected a value after '='"); return; }

            ValidateRHSInPlace(toks, ref pos, declaredType, errors);

            if (pos >= toks.Count || toks[pos][1] != "Semicolon")
                errors.Add("Expected ';' at end of statement");
            else pos++;

            if (pos < toks.Count)
                errors.Add($"Unexpected token '{toks[pos][0]}' after semicolon");
        }

        private void ValidateRHSInPlace(List<string[]> toks, ref int pos, string declaredType, List<string> errors)
        {
            if (declaredType == "string")
            {
                if (pos >= toks.Count || toks[pos][1] != "Double Quote")
                { errors.Add("Expected a string value in double quotes for type 'string'"); return; }
                pos++;
                while (pos < toks.Count && toks[pos][1] != "Double Quote" && toks[pos][1] != "Semicolon")
                    pos++;
                if (pos >= toks.Count || toks[pos][1] != "Double Quote")
                    errors.Add("String value is missing a closing double quote");
                else pos++;
                return;
            }

            if (declaredType == "bool")
            {
                if (pos >= toks.Count)
                { errors.Add("Expected 'true' or 'false' for type 'bool'"); return; }
                string val = toks[pos][0];
                if (val != "true" && val != "false")
                    errors.Add($"Type mismatch: cannot assign '{val}' to type 'bool' (expected true or false)");
                pos++;
                return;
            }

            if (declaredType == "char")
            {
                if (pos >= toks.Count || toks[pos][1] != "Single Quote")
                { errors.Add("Expected a char value in single quotes for type 'char'"); return; }
                pos++;
                if (pos >= toks.Count || toks[pos][1] == "Single Quote")
                { errors.Add("Expected a character inside single quotes"); return; }
                pos++;
                if (pos >= toks.Count || toks[pos][1] != "Single Quote")
                    errors.Add("Char value is missing a closing single quote");
                else pos++;
                return;
            }

            if (pos >= toks.Count || !IsVarOrNum(toks[pos]))
            {
                errors.Add($"Expected a number or variable after '=', got '{(pos < toks.Count ? toks[pos][0] : "nothing")}'");
                return;
            }

            if (toks[pos][1] == "Variable" && !_symbolTable.ContainsKey(toks[pos][0]))
                errors.Add($"Variable '{toks[pos][0]}' is used before being declared");

            if (declaredType == "int" && toks[pos][1] == "Number" && toks[pos][0].Contains("."))
                errors.Add($"Type mismatch: cannot assign float value '{toks[pos][0]}' to type 'int'");

            pos++;

            while (pos < toks.Count && toks[pos][1] == "Operator")
            {
                pos++;
                if (pos >= toks.Count || !IsVarOrNum(toks[pos]))
                { errors.Add("Expected a variable or number after operator"); return; }

                if (toks[pos][1] == "Variable" && !_symbolTable.ContainsKey(toks[pos][0]))
                    errors.Add($"Variable '{toks[pos][0]}' is used before being declared");

                if (declaredType == "int" && toks[pos][1] == "Number" && toks[pos][0].Contains("."))
                    errors.Add($"Type mismatch: cannot assign float value '{toks[pos][0]}' to type 'int'");

                pos++;
            }
        }

        private static bool IsComparator(string[] tok)
        {
            return tok[1] == "Less than" || tok[1] == "Greater than" || tok[1] == "Equal";
        }

        private static bool IsVarOrNum(string[] tok)
        {
            if (tok[1] == "Variable") return true;
            if (tok[1] == "Number") return true;
            return double.TryParse(tok[0], out _);
        }

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

        private void AppendLine(string text, Color color)
        {
            rtbResult.SelectionStart = rtbResult.TextLength;
            rtbResult.SelectionLength = 0;
            rtbResult.SelectionColor = color;
            rtbResult.AppendText(text + Environment.NewLine);
            rtbResult.SelectionColor = rtbResult.ForeColor;
        }
    }
}