using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualBasic.Logging;

namespace MultiObfuscator
{
    public partial class Form1 : Form
    {
        // Hold the settings instance.
        private ObfuscatorSettings obfuscatorSettings = new ObfuscatorSettings();

        public Form1()
        {
            InitializeComponent();
        }

        public void Log(string message, string level = "info")
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action(() => Log(message, level)));
                return;
            }

            // Get the current time
            string time = DateTime.Now.ToString("HH:mm:ss");
            string formattedMessage = $"[{time}] {message}";

            // Set color based on log level
            Color logColor = Color.White;
            switch (level.ToLower())
            {
                case "info":
                    logColor = Color.Blue;
                    break;
                case "warning":
                    logColor = Color.Orange;
                    break;
                case "error":
                    logColor = Color.Red;
                    break;
                case "success":
                    logColor = Color.Green;
                    break;
                case "debug":
                    logColor = Color.White;
                    break;
            }

            // Append text with color
            AppendTextWithColor(txtLog, formattedMessage + Environment.NewLine, logColor);
        }

        // Helper function to add colored text
        private void AppendTextWithColor(RichTextBox box, string text, Color color)
        {
            box.SelectionStart = box.TextLength;
            box.SelectionLength = 0;
            box.SelectionColor = color;
            box.AppendText(text);
            box.SelectionColor = box.ForeColor; // Reset color
        }

        private void obfuscate()
        {
            try
            {
                // Clear the log
                txtLog.Clear();

                Log("Starting obfuscation process...", "info");

                string code = File.ReadAllText(txtPathToObfuscate.Text);
                Log("File successfully read.", "success");

                SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
                var root = tree.GetRoot();

                var errors = tree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
                if (errors.Any())
                {
                    Log("Syntax errors detected. Obfuscation aborted.", "error");
                    MessageBox.Show($"Syntax errors detected:\n{string.Join("\n", errors)}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var obfuscator = new Obfuscator(Log);
                var obfuscatedRoot = obfuscator.Visit(root).NormalizeWhitespace();
                Log("Obfuscation completed successfully.", "success");

                txtObfuscated.Text = obfuscatedRoot.ToFullString();
            }
            catch (Exception ex)
            {
                Log($"[ERROR] An unexpected error occurred: {ex.Message}", "error");
            }
        }

        private void btnObfuscate_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtPathToObfuscate.Text))
            {
                Log("No file selected for obfuscation.", "error");
                MessageBox.Show("Please select a file to obfuscate", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Log("Obfuscation started.", "info");
            updateTextbox();
            obfuscate();
        }

        private void updateTextbox()
        {
            txtOrginal.Text = File.ReadAllText(txtPathToObfuscate.Text);
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "C# files (*.cs)|*.cs";
            dialog.Title = "Select a C# file to obfuscate";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                Log($"File selected: {dialog.FileName}", "info");
                txtPathToObfuscate.Text = dialog.FileName;
                updateTextbox();
            }
            else
            {
                Log("No file selected.", "warning");
            }
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            using (var settingsForm = new SettingsForm(obfuscatorSettings))
            {
                if (settingsForm.ShowDialog() == DialogResult.OK)
                {
                    // The obfuscatorSettings object is updated automatically.
                    MessageBox.Show("Settings updated.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }

    class Obfuscator : CSharpSyntaxRewriter
    {
        // Global mapping för klasser, metoder etc.
        private Dictionary<string, string> nameMap = new Dictionary<string, string>();
        // En stack för lokala (scope‑specifika) namn
        private Stack<Dictionary<string, string>> localScopeStack = new Stack<Dictionary<string, string>>();
        private Random random = new Random();
        private Action<string, string> log;
        private bool decryptStringInjected = false;
        private string decryptStringMethodName = "M_mgbkf";
        private string decryptStringVariableName = "V_homft";
        private string decryptStringVariable2Name = "V_xkgpd";

        private ObfuscatorSettings settings; // New settings object

        private static readonly HashSet<string> systemIdentifiers = new HashSet<string>
        {
            "System", "Console", "Microsoft", "Convert", "Encoding", "Environment",
            "DateTime", "Exception", "Task", "List", "Enumerable", "ToUpperInvariant",
            "Trim", "ToUpper", "Substring", "ToLower", "Where", "ToList", "Join",
            "Parse", "Length"
        };

        public Obfuscator(Action<string, string> logAction, ObfuscatorSettings settings = null)
        {
            this.log = logAction;
            this.settings = settings ?? new ObfuscatorSettings();
        }

        #region Hjälpfunktioner för namn

        private bool IsMainMethod(MethodDeclarationSyntax method)
        {
            return method.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)) &&
                   method.ReturnType is PredefinedTypeSyntax returnType &&
                   returnType.Keyword.IsKind(SyntaxKind.VoidKeyword);
        }
        private void ProcessMethod(MethodDeclarationSyntax method)
        {
            if (method.ExplicitInterfaceSpecifier != null ||
                method.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)))
            {
                log($"[SkipMethod] Preserving {method.Identifier.Text}", "info");
                return;
            }

            var methodName = method.Identifier.Text;
            if (methodName == "Main" && IsMainMethod(method)) return;

            AddNameMap(methodName, "M");
            ProcessParameters(method.ParameterList);
            ProcessVariables(method.Body);
        }

        private string GenerateRandomName()
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var generated = new string(Enumerable.Repeat(chars, 6)
                .Select(s => s[random.Next(s.Length)]).ToArray());
            log($"[GenerateRandomName] -> {generated}", "debug");
            return generated;
        }

        // Global mapping (för klasser, metoder, fält etc.)
        private void AddGlobalMapping(string originalName, string prefix)
        {
            if (!nameMap.ContainsKey(originalName))
            {
                string newName = prefix + "_" + GenerateRandomName();
                nameMap[originalName] = newName;
                log($"[AddGlobalMapping] '{originalName}' -> '{newName}'", "success");
            }
            else
            {
                log($"[AddGlobalMapping] already exists: '{originalName}' -> '{nameMap[originalName]}'", "info");
            }
        }

        private string LookupGlobalMapping(string originalName)
        {
            if (nameMap.TryGetValue(originalName, out var newName))
            {
                log($"[LookupGlobalMapping] Found mapping for '{originalName}' -> '{newName}'", "debug");
                return newName;
            }
            else
            {
                log($"[LookupGlobalMapping] WARNING: No mapping found for '{originalName}'", "warning");
                return null;
            }
        }

        // Lokal (scope‑specifik) mapping
        private void PushLocalScope()
        {
            localScopeStack.Push(new Dictionary<string, string>());
        }
        private void PopLocalScope()
        {
            localScopeStack.Pop();
        }

        private void AddLocalMapping(string originalName, string newName)
        {
            if (localScopeStack.Count > 0)
            {
                localScopeStack.Peek()[originalName] = newName;
                log($"[AddLocalMapping] '{originalName}' -> '{newName}'", "debug");
            }
        }

        private string LookupLocalMapping(string originalName)
        {
            foreach (var scope in localScopeStack)
            {
                if (scope.TryGetValue(originalName, out var newName))
                {
                    log($"[LookupLocalMapping] Found mapping for '{originalName}' -> '{newName}'", "debug");
                    return newName;
                }
            }
            return null;
        }

        #endregion

        #region Överskrivningar för lokala scopes
        // Varje block får sin egen lokala scope
        public override SyntaxNode VisitBlock(BlockSyntax node)
        {
            PushLocalScope();
            var visitedBlock = (BlockSyntax)base.VisitBlock(node);
            PopLocalScope();
            return visitedBlock;
        }

        // Parametrar är lokala – lägg mapping i den aktuella scopen
        public override SyntaxNode VisitParameter(ParameterSyntax node)
        {
            string originalName = node.Identifier.Text;
            string newName = LookupLocalMapping(originalName);
            if (newName == null)
            {
                newName = "P_" + GenerateRandomName();
                AddLocalMapping(originalName, newName);
            }
            node = node.WithIdentifier(SyntaxFactory.Identifier(newName));
            return base.VisitParameter(node);
        }

        // Variabeldeklarationer – skilj mellan fält (globalt) och lokala variabler
        public override SyntaxNode VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            string originalName = node.Identifier.Text;
            bool isField = node.Parent?.Parent is ClassDeclarationSyntax;
            string newName = null;
            if (isField)
            {
                newName = LookupGlobalMapping(originalName);
                if (newName == null)
                {
                    newName = "V_" + GenerateRandomName();
                    AddGlobalMapping(originalName, "V");
                    newName = LookupGlobalMapping(originalName);
                }
            }
            else
            {
                newName = LookupLocalMapping(originalName);
                if (newName == null)
                {
                    newName = "V_" + GenerateRandomName();
                    AddLocalMapping(originalName, newName);
                }
            }
            var visited = (VariableDeclaratorSyntax)base.VisitVariableDeclarator(node);
            visited = visited.WithIdentifier(SyntaxFactory.Identifier(newName));
            return visited;
        }

        // IdentifierName: kolla först i lokala scopes, annars i global mapping
        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
        {
            string original = node.Identifier.Text;
            string newName = LookupLocalMapping(original) ?? LookupGlobalMapping(original);
            if (newName != null)
            {
                node = node.WithIdentifier(SyntaxFactory.Identifier(newName));
            }
            return base.VisitIdentifierName(node);
        }

        // Vid metodanrop kontrolleras även lokala mappings
        public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (node.Expression is IdentifierNameSyntax identifier)
            {
                string originalName = identifier.Identifier.Text;
                // Om namnet redan är obfuskerat, hoppa över
                if (originalName.StartsWith("M_") || originalName.StartsWith("P_")
                    || originalName.StartsWith("V_") || originalName.StartsWith("C_"))
                {
                    return base.VisitInvocationExpression(node);
                }
                string newName = LookupLocalMapping(originalName) ?? LookupGlobalMapping(originalName);
                if (newName != null)
                {
                    log($"[VisitInvocationExpression] Replacing invocation '{originalName}' -> '{newName}'", "info");
                    node = node.WithExpression(SyntaxFactory.IdentifierName(newName));
                }
            }
            return base.VisitInvocationExpression(node);
        }

        #endregion

        #region Övriga överskrivningar (global mapping)

        public override SyntaxNode VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            if (node.IsKind(SyntaxKind.StringLiteralExpression))
            {
                if (node.Parent is CaseSwitchLabelSyntax)
                {
                    return node;
                }
                string originalString = node.Token.ValueText;
                if (!settings.EnableStringSplitting || originalString.Length <= 4)
                {
                    string encryptedString = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(originalString));
                    log($"[VisitLiteralExpression] Encrypting '{originalString}' -> {decryptStringMethodName}(\"{encryptedString}\")", "info");
                    return SyntaxFactory.InvocationExpression(
                        SyntaxFactory.IdentifierName(decryptStringMethodName),
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Argument(
                                    SyntaxFactory.LiteralExpression(
                                        SyntaxKind.StringLiteralExpression,
                                        SyntaxFactory.Literal(encryptedString)
                                    )
                                )
                            )
                        )
                    );
                }
                else
                {
                    log($"[VisitLiteralExpression] Splitting and encrypting '{originalString}'", "info");
                    int maxPieces = Math.Min(5, originalString.Length);
                    int pieces = random.Next(2, maxPieces + 1);
                    int pieceLength = originalString.Length / pieces;
                    List<string> splitPieces = new List<string>();
                    int index = 0;
                    for (int i = 0; i < pieces; i++)
                    {
                        if (i == pieces - 1)
                        {
                            splitPieces.Add(originalString.Substring(index));
                        }
                        else
                        {
                            splitPieces.Add(originalString.Substring(index, pieceLength));
                            index += pieceLength;
                        }
                    }
                    List<ExpressionSyntax> partExpressions = new List<ExpressionSyntax>();
                    foreach (var piece in splitPieces)
                    {
                        string encodedPiece = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(piece));
                        var decryptCall = SyntaxFactory.InvocationExpression(
                            SyntaxFactory.IdentifierName(decryptStringMethodName),
                            SyntaxFactory.ArgumentList(
                                SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory.Argument(
                                        SyntaxFactory.LiteralExpression(
                                            SyntaxKind.StringLiteralExpression,
                                            SyntaxFactory.Literal(encodedPiece)
                                        )
                                    )
                                )
                            )
                        );
                        partExpressions.Add(decryptCall);
                    }
                    ExpressionSyntax concatenated = partExpressions[0];
                    for (int i = 1; i < partExpressions.Count; i++)
                    {
                        concatenated = SyntaxFactory.BinaryExpression(
                            SyntaxKind.AddExpression,
                            concatenated,
                            partExpressions[i]
                        );
                    }
                    log($"[VisitLiteralExpression - StringSplitting] '{originalString}' split into {pieces} parts.", "info");
                    return concatenated;
                }
            }
            return base.VisitLiteralExpression(node);
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            string originalClassName = node.Identifier.Text;
            if (IsSystemType(originalClassName)) return base.VisitClassDeclaration(node);
            AddGlobalMapping(originalClassName, "C");
            foreach (var member in node.Members)
            {
                if (member is MethodDeclarationSyntax method)
                {
                    ProcessMethod(method);
                }
            }
            var visitedClass = (ClassDeclarationSyntax)base.VisitClassDeclaration(node);
            visitedClass = visitedClass.WithIdentifier(GetRenamedIdentifier(originalClassName));
            if (!decryptStringInjected)
            {
                log($"[VisitClassDeclaration] Injecting DecryptString method ({decryptStringMethodName}).", "info");
                visitedClass = visitedClass.AddMembers(CreateDecryptMethod());
                decryptStringInjected = true;
            }
            return visitedClass;
        }

        // Vi fortsätter att använda din kontrollflödesomvandling i metoder
        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            // Remove the check for "Main" so that even Main is transformed
            if (node.ExplicitInterfaceSpecifier != null ||
                node.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)) ||
                node.Body == null ||
                !settings.EnableControlFlowFlattening)
            {
                return base.VisitMethodDeclaration(node);
            }
            // Create a local scope for the method’s body.
            PushLocalScope();
            MethodDeclarationSyntax transformedNode = (MethodDeclarationSyntax)base.VisitMethodDeclaration(node);
            bool isNonVoid = true;
            if (transformedNode.ReturnType is PredefinedTypeSyntax pts &&
                pts.Keyword.IsKind(SyntaxKind.VoidKeyword))
            {
                isNonVoid = false;
            }
            // Create two state variables (names are randomly generated for the state and result)
            string stateVarName = "V_" + GenerateRandomName();
            string resultVarName = "V_" + GenerateRandomName();
            AddLocalMapping(stateVarName, stateVarName);
            AddLocalMapping(resultVarName, resultVarName);
            List<StatementSyntax> newStatements = new List<StatementSyntax>();

            // 1. Declare the state variable (the “switch selector”)
            var stateDeclaration = SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)))
                .WithVariables(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(stateVarName))
                        .WithInitializer(
                            SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.LiteralExpression(
                                    SyntaxKind.NumericLiteralExpression,
                                    SyntaxFactory.Literal(0)
                                )
                            )
                        )
                    )
                )
            );
            newStatements.Add(stateDeclaration);

            // 2. For non-void methods, declare a result variable.
            if (isNonVoid)
            {
                var resultDeclaration = SyntaxFactory.LocalDeclarationStatement(
                    SyntaxFactory.VariableDeclaration(transformedNode.ReturnType)
                    .WithVariables(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(resultVarName))
                            .WithInitializer(
                                SyntaxFactory.EqualsValueClause(
                                    SyntaxFactory.DefaultExpression(transformedNode.ReturnType)
                                )
                            )
                        )
                    )
                );
                newStatements.Add(resultDeclaration);
            }

            // 3. Transform each original statement into a switch section.
            var originalStatements = transformedNode.Body.Statements;
            List<SwitchSectionSyntax> switchSections = new List<SwitchSectionSyntax>();
            int caseIndex = 0;
            foreach (var stmt in originalStatements)
            {
                var caseLabel = SyntaxFactory.CaseSwitchLabel(
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal(caseIndex)
                    )
                );
                List<StatementSyntax> caseStatements = new List<StatementSyntax>();
                if (stmt is ReturnStatementSyntax returnStmt)
                {
                    if (isNonVoid)
                    {
                        var assignResult = SyntaxFactory.ExpressionStatement(
                            SyntaxFactory.AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                SyntaxFactory.IdentifierName(resultVarName),
                                returnStmt.Expression ?? SyntaxFactory.DefaultExpression(transformedNode.ReturnType)
                            )
                        );
                        caseStatements.Add(assignResult);
                    }
                    else
                    {
                        caseStatements.Add(stmt);
                    }
                    var setTerminate = SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            SyntaxFactory.IdentifierName(stateVarName),
                            SyntaxFactory.PrefixUnaryExpression(
                                SyntaxKind.UnaryMinusExpression,
                                SyntaxFactory.LiteralExpression(
                                    SyntaxKind.NumericLiteralExpression,
                                    SyntaxFactory.Literal(1)
                                )
                            )
                        )
                    );
                    caseStatements.Add(setTerminate);
                }
                else
                {
                    caseStatements.Add(stmt);
                    var setNext = SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            SyntaxFactory.IdentifierName(stateVarName),
                            SyntaxFactory.LiteralExpression(
                                SyntaxKind.NumericLiteralExpression,
                                SyntaxFactory.Literal(caseIndex + 1)
                            )
                        )
                    );
                    caseStatements.Add(setNext);
                }
                caseStatements.Add(SyntaxFactory.BreakStatement());
                var switchSection = SyntaxFactory.SwitchSection(
                    SyntaxFactory.List<SwitchLabelSyntax>(new[] { caseLabel }),
                    SyntaxFactory.List(caseStatements)
                );
                switchSections.Add(switchSection);
                caseIndex++;
            }

            // 4. Create a default switch section.
            var defaultSection = SyntaxFactory.SwitchSection(
                SyntaxFactory.List<SwitchLabelSyntax>(new[] { SyntaxFactory.DefaultSwitchLabel() }),
                SyntaxFactory.List(new StatementSyntax[] { SyntaxFactory.BreakStatement() })
            );
            switchSections.Add(defaultSection);

            // 5. Build the switch statement that “drives” the flattened control flow.
            var switchStatement = SyntaxFactory.SwitchStatement(
                SyntaxFactory.IdentifierName(stateVarName),
                SyntaxFactory.List(switchSections)
            );

            // 6. Create a while–loop that repeatedly executes the switch.
            var whileLoop = SyntaxFactory.WhileStatement(
                SyntaxFactory.BinaryExpression(
                    SyntaxKind.NotEqualsExpression,
                    SyntaxFactory.IdentifierName(stateVarName),
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal(-1)
                    )
                ),
                SyntaxFactory.Block(switchStatement)
            );
            newStatements.Add(whileLoop);

            // 7. For non-void methods, add a return statement.
            if (isNonVoid)
            {
                newStatements.Add(SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName(resultVarName)));
            }
            var newBody = SyntaxFactory.Block(newStatements);

            // **** NEW CODE: Inject missing variable declarations for V_QGoioV and V_ORjvqQ ****
            // If the new body’s text contains these identifiers but no local declaration,
            // then insert declarations at the very beginning.
            var bodyText = newBody.ToFullString();
            List<StatementSyntax> injections = new List<StatementSyntax>();
            if (bodyText.Contains("V_QGoioV") && !bodyText.Contains("int V_QGoioV"))
            {
                injections.Add(SyntaxFactory.ParseStatement("int V_QGoioV = 0;"));
            }
            if (bodyText.Contains("V_ORjvqQ") && !bodyText.Contains("int V_ORjvqQ"))
            {
                injections.Add(SyntaxFactory.ParseStatement("int V_ORjvqQ = 0;"));
            }
            if (injections.Any())
            {
                newBody = newBody.WithStatements(newBody.Statements.InsertRange(0, injections));
            }
            // **** End of injection ****

            transformedNode = transformedNode.WithBody(newBody);
            transformedNode = transformedNode.WithIdentifier(GetRenamedIdentifier(transformedNode.Identifier.Text));
            PopLocalScope();
            return transformedNode;
        }

        public override SyntaxNode VisitInterpolation(InterpolationSyntax node)
        {
            var visited = (InterpolationSyntax)base.VisitInterpolation(node);
            return visited.WithExpression((ExpressionSyntax)Visit(visited.Expression));
        }

        public override SyntaxNode VisitGenericName(GenericNameSyntax node)
        {
            string original = node.Identifier.Text;
            string newName = LookupLocalMapping(original) ?? LookupGlobalMapping(original);
            return newName != null ? node.WithIdentifier(SyntaxFactory.Identifier(newName)) : node;
        }

        #endregion

        #region Övrigt

        public override SyntaxNode VisitSwitchStatement(SwitchStatementSyntax node)
        {
            log($"[VisitSwitchStatement] Obfuscating switch statement.", "info");
            // Exempel på omvandling av switch till en while-loop med slumpade case-värden

            string switchVarName = GenerateRandomName(); // initial state-variabel
            AddGlobalMapping(switchVarName, "V"); // hantera globalt
            string stateVarName = GenerateRandomName();
            AddLocalMapping(stateVarName, stateVarName); // behandla state-variabeln som lokal

            var switchCases = node.Sections
                .Select((section, index) => new { Case = section, Index = index })
                .OrderBy(x => random.Next())
                .ToList();
            var caseMappings = switchCases.ToDictionary(x => x.Index, x => random.Next(1000, 9999));

            var caseStatements = new List<StatementSyntax>();

            // Deklarera state-variabeln
            var stateDeclaration = SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)))
                .WithVariables(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(LookupLocalMapping(stateVarName) ?? stateVarName))
                        .WithInitializer(
                            SyntaxFactory.EqualsValueClause(SyntaxFactory.IdentifierName(LookupGlobalMapping(switchVarName) ?? switchVarName))
                        )
                    )
                )
            );
            caseStatements.Add(stateDeclaration);

            var switchSections = switchCases.Select(x =>
            {
                var caseLabel = SyntaxFactory.CaseSwitchLabel(
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal(caseMappings[x.Index])
                    )
                );
                var statements = x.Case.Statements
                    .Select(stmt => (StatementSyntax)Visit(stmt))
                    .ToList();
                if (statements.Count > 0)
                {
                    statements.Add(
                        SyntaxFactory.ExpressionStatement(
                            SyntaxFactory.AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                SyntaxFactory.IdentifierName(LookupLocalMapping(stateVarName) ?? stateVarName),
                                SyntaxFactory.LiteralExpression(
                                    SyntaxKind.NumericLiteralExpression,
                                    SyntaxFactory.Literal(caseMappings.ContainsKey(x.Index + 1) ? caseMappings[x.Index + 1] : -1)
                                )
                            )
                        )
                    );
                }
                statements.Add(SyntaxFactory.BreakStatement());
                return SyntaxFactory.SwitchSection(
                    SyntaxFactory.List<SwitchLabelSyntax>(new[] { caseLabel }),
                    SyntaxFactory.List(statements)
                );
            }).ToList();

            var defaultSection = SyntaxFactory.SwitchSection(
                SyntaxFactory.List<SwitchLabelSyntax>(new[] { SyntaxFactory.DefaultSwitchLabel() }),
                SyntaxFactory.List(new StatementSyntax[] { SyntaxFactory.BreakStatement() })
            );
            switchSections.Add(defaultSection);

            var obfuscatedSwitch = SyntaxFactory.SwitchStatement(
                SyntaxFactory.IdentifierName(LookupLocalMapping(stateVarName) ?? stateVarName),
                SyntaxFactory.List(switchSections)
            );
            var whileLoop = SyntaxFactory.WhileStatement(
                SyntaxFactory.BinaryExpression(
                    SyntaxKind.NotEqualsExpression,
                    SyntaxFactory.IdentifierName(LookupLocalMapping(stateVarName) ?? stateVarName),
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal(-1)
                    )
                ),
                SyntaxFactory.Block(obfuscatedSwitch)
            );
            caseStatements.Add(whileLoop);
            return SyntaxFactory.Block(caseStatements);
        }

        private bool IsSystemType(string identifier) =>
            systemIdentifiers.Contains(identifier) ||
            identifier.StartsWith("System.") ||
            identifier.StartsWith("Microsoft.");

        private MethodDeclarationSyntax CreateDecryptMethod() =>
            SyntaxFactory.ParseMemberDeclaration($@"
                private static string {decryptStringMethodName}(string {decryptStringVariableName})
                {{
                    if (string.IsNullOrEmpty({decryptStringVariableName})) return string.Empty;
                    byte[] {decryptStringVariable2Name} = Convert.FromBase64String({decryptStringVariableName});
                    return System.Text.Encoding.UTF8.GetString({decryptStringVariable2Name});
                }}") as MethodDeclarationSyntax;

        private SyntaxToken GetRenamedIdentifier(string originalName)
        {
            string newName = LookupGlobalMapping(originalName) ?? originalName;
            return SyntaxFactory.Identifier(newName);
        }

        #endregion

        private void AddNameMap(string originalName, string prefix)
        {
            if (!nameMap.ContainsKey(originalName))
            {
                string newName = prefix + "_" + GenerateRandomName();
                nameMap[originalName] = newName;
                log($"[AddNameMap] '{originalName}' -> '{newName}'", "success");
            }
            else
            {
                log($"[AddNameMap] already exists: '{originalName}' -> '{nameMap[originalName]}'", "info");
            }
        }

        private string LookupName(string originalName)
        {
            if (nameMap.TryGetValue(originalName, out var newName))
            {
                log($"[LookupName] Found mapping for '{originalName}' -> '{newName}'", "debug");
                return newName;
            }
            else
            {
                log($"[LookupName] WARNING: No mapping found for '{originalName}'", "warning");
                return null;
            }
        }

        private void ProcessParameters(ParameterListSyntax parameterList)
        {
            foreach (var param in parameterList.Parameters)
            {
                AddNameMap(param.Identifier.Text, "P");
            }
        }

        private void ProcessVariables(SyntaxNode body)
        {
            if (body == null) return;

            foreach (var variable in body.DescendantNodes().OfType<VariableDeclaratorSyntax>())
            {
                AddNameMap(variable.Identifier.Text, "V");
            }
        }

        private StatementSyntax GenerateDeadCodeBlock()
        {
            var condition = SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression);
            int randomValue = random.Next(0, 100);
            string randomString = GenerateRandomName();
            var dummyDeclaration = SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)))
                .WithVariables(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("V_" + randomString))
                        .WithInitializer(
                            SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.LiteralExpression(
                                    SyntaxKind.NumericLiteralExpression,
                                    SyntaxFactory.Literal(randomValue)
                                )
                            )
                        )
                    )
                )
            ).NormalizeWhitespace();  // Ensure formatting

            var block = SyntaxFactory.Block(dummyDeclaration);
            return SyntaxFactory.IfStatement(condition, block);
        }

    }
}
