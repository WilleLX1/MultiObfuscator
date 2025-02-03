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
        private Dictionary<string, string> nameMap = new Dictionary<string, string>();
        private Random random = new Random();
        private Action<string, string> log;
        private bool decryptStringInjected = false; 
        private string decryptStringMethodName = "M_xxxxx";
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


        private string GenerateRandomName()
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var generated = new string(Enumerable.Repeat(chars, 6)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            log($"[GenerateRandomName] -> {generated}", "debug");
            return generated;
        }

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

        public override SyntaxNode VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            if (node.IsKind(SyntaxKind.StringLiteralExpression))
            {
                if (node.Parent is CaseSwitchLabelSyntax)
                {
                    return node;
                }
                string originalString = node.Token.ValueText;

                // If string splitting is disabled, simply encrypt the whole string.
                if (!settings.EnableStringSplitting || originalString.Length <= 4)
                {
                    string encryptedString = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(originalString));
                    log($"[VisitLiteralExpression] Encrypting '{originalString}' -> M_xxxxx(\"{encryptedString}\")", "info");

                    return SyntaxFactory.InvocationExpression(
                        SyntaxFactory.IdentifierName("M_xxxxx"),
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
                else // If enabled, split the string.
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
                            SyntaxFactory.IdentifierName("M_xxxxx"),
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

        public override SyntaxNode VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            var originalName = node.Identifier.Text;
            AddNameMap(originalName, "V");

            var visited = (VariableDeclaratorSyntax)base.VisitVariableDeclarator(node);
            var newVarName = LookupName(originalName);

            if (newVarName != null)
            {
                log($"[VisitVariableDeclarator] Renaming variable '{originalName}' -> '{newVarName}'", "debug");
                visited = visited.WithIdentifier(SyntaxFactory.Identifier(newVarName));
            }

            return visited;
        }

        public override SyntaxNode VisitParameter(ParameterSyntax node)
        {
            var originalName = node.Identifier.Text;
            log($"[VisitParameter] param '{originalName}' ...", "info");

            var newName = LookupName(originalName);
            if (newName != null)
            {
                node = node.WithIdentifier(SyntaxFactory.Identifier(newName));
            }

            return base.VisitParameter(node);
        }

        public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (node.Expression is IdentifierNameSyntax identifier)
            {
                string originalName = identifier.Identifier.Text;

                // Skip lookup for already-obfuscated names.
                if (originalName.StartsWith("M_") || originalName.StartsWith("P_")
                    || originalName.StartsWith("V_") || originalName.StartsWith("C_"))
                {
                    return base.VisitInvocationExpression(node);
                }

                string newName = LookupName(originalName);
                if (newName != null)
                {
                    log($"[VisitInvocationExpression] Replacing invocation '{originalName}' -> '{newName}'", "info");
                    node = node.WithExpression(SyntaxFactory.IdentifierName(newName));
                }
            }

            return base.VisitInvocationExpression(node);
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var originalClassName = node.Identifier.Text;
            if (IsSystemType(originalClassName)) return base.VisitClassDeclaration(node);

            AddNameMap(originalClassName, "C");

            foreach (var member in node.Members)
            {
                if (member is MethodDeclarationSyntax method)
                {
                    ProcessMethod(method);
                }
            }

            // Process the class normally.
            var visitedClass = (ClassDeclarationSyntax)base.VisitClassDeclaration(node);
            visitedClass = visitedClass.WithIdentifier(GetRenamedIdentifier(originalClassName));

            if (!decryptStringInjected)
            {
                log("[VisitClassDeclaration] Injecting DecryptString method (M_xxxxx).", "info");
                visitedClass = visitedClass.AddMembers(CreateDecryptMethod());
                decryptStringInjected = true;
            }

            // === NEW: Inject a static field for V_xxxxx ===
            // This ensures that any references (e.g. in dead code) to "V_xxxxx" are valid.
            // Generate a random string value for the field.
            string randomString = GenerateRandomName();

            bool hasV_xxxxxDeclaration = visitedClass.Members
                .OfType<FieldDeclarationSyntax>()
                .Any(f => f.Declaration.Variables.Any(v => v.Identifier.Text == "V_" + randomString));
            // === End of NEW code ===

            return visitedClass;
        }

        public override SyntaxNode VisitBlock(BlockSyntax node)
        {
            var visitedBlock = (BlockSyntax)base.VisitBlock(node);
            var statements = visitedBlock.Statements.ToList();

            if (statements.Count > 0 && random.NextDouble() < settings.DeadCodeProbability)
            {
                var returnIndices = statements
                    .Select((stmt, index) => new { stmt, index })
                    .Where(x => x.stmt is ReturnStatementSyntax)
                    .Select(x => x.index)
                    .ToList();

                int insertionIndex;
                if (returnIndices.Any())
                {
                    insertionIndex = random.Next(0, returnIndices.Min());
                }
                else
                {
                    insertionIndex = random.Next(0, statements.Count + 1);
                }
                statements.Insert(insertionIndex, GenerateDeadCodeBlock());
            }

            return visitedBlock.WithStatements(SyntaxFactory.List(statements));
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

        private bool IsMainMethod(MethodDeclarationSyntax method)
        {
            return method.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)) &&
                   method.ReturnType is PredefinedTypeSyntax returnType &&
                   returnType.Keyword.IsKind(SyntaxKind.VoidKeyword);
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

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            // If the method is public, has an interface specifier, has no body,
            // or is Main (if you wish to leave Main untouched), or if flattening is disabled:
            if (node.ExplicitInterfaceSpecifier != null ||
                node.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)) ||
                node.Body == null ||
                (node.Identifier.Text == "Main" && IsMainMethod(node)) ||
                !settings.EnableControlFlowFlattening)
            {
                return base.VisitMethodDeclaration(node);
            }

            // Otherwise, perform the control–flow flattening transformation.
            // (The transformation code remains essentially the same as before.)
            MethodDeclarationSyntax transformedNode = (MethodDeclarationSyntax)base.VisitMethodDeclaration(node);

            bool isNonVoid = true;
            if (transformedNode.ReturnType is PredefinedTypeSyntax pts &&
                pts.Keyword.IsKind(SyntaxKind.VoidKeyword))
            {
                isNonVoid = false;
            }

            string stateVarName = "V_" + GenerateRandomName();
            string resultVarName = "V_" + GenerateRandomName();

            List<StatementSyntax> newStatements = new List<StatementSyntax>();

            // 1. Insert state variable declaration.
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

            // 2. For non–void methods, declare a result variable.
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

            // 3. Process each original statement into a switch section.
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

            // 5. Build the switch statement.
            var switchStatement = SyntaxFactory.SwitchStatement(
                SyntaxFactory.IdentifierName(stateVarName),
                SyntaxFactory.List(switchSections)
            );

            // 6. Build a while loop that runs until state equals -1.
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

            // 7. For non–void methods, add a final return.
            if (isNonVoid)
            {
                newStatements.Add(SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName(resultVarName)));
            }

            var newBody = SyntaxFactory.Block(newStatements);
            var newMethod = transformedNode.WithBody(newBody);
            newMethod = newMethod.WithIdentifier(GetRenamedIdentifier(transformedNode.Identifier.Text));
            transformedNode = (MethodDeclarationSyntax)newMethod.NormalizeWhitespace();
            return transformedNode;
        }

        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
        {
            var original = node.Identifier.Text;
            var newName = LookupName(original);

            log($"[VisitIdentifierName] '{original}' -> '{newName}'", "info");

            return newName != null
                ? node.WithIdentifier(SyntaxFactory.Identifier(newName))
                : node;
        }

        public override SyntaxNode VisitInterpolation(InterpolationSyntax node)
        {
            var visited = (InterpolationSyntax)base.VisitInterpolation(node);
            return visited.WithExpression((ExpressionSyntax)Visit(visited.Expression));
        }

        public override SyntaxNode VisitGenericName(GenericNameSyntax node)
        {
            var original = node.Identifier.Text;
            var newName = LookupName(original);

            return newName != null
                ? node.WithIdentifier(SyntaxFactory.Identifier(newName))
                : node;
        }

        private bool IsSystemType(string identifier) =>
            systemIdentifiers.Contains(identifier) ||
            identifier.StartsWith("System.") ||
            identifier.StartsWith("Microsoft.");

        private MethodDeclarationSyntax CreateDecryptMethod() =>
            SyntaxFactory.ParseMemberDeclaration(@"
                private static string M_xxxxx(string V_xxxxx)
                {
                    if (string.IsNullOrEmpty(V_xxxxx)) return string.Empty;
                    byte[] V_xxxx = Convert.FromBase64String(V_xxxxx);
                    return System.Text.Encoding.UTF8.GetString(V_xxxx);
                }") as MethodDeclarationSyntax;

        private SyntaxToken GetRenamedIdentifier(string originalName)
        {
            var newName = LookupName(originalName);
            return SyntaxFactory.Identifier(newName ?? originalName);
        }
    }
}
