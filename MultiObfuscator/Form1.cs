using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace MultiObfuscator
{
    public partial class Form1 : Form
    {
        #region Fält

        private List<ObfuscationStep> _steps;
        private int _currentStepIndex;
        private SyntaxTree _currentTree;
        private readonly ObfuscatorSettings _obfuscatorSettings = new ObfuscatorSettings();

        #endregion

        #region Konstruktor

        public Form1()
        {
            InitializeComponent();
            chkSlowMode.CheckedChanged += (s, e) => btnNextStep.Enabled = chkSlowMode.Checked;
        }

        #endregion

        #region Eventhanterare

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Filter = "C# Files (*.cs)|*.cs|All Files (*.*)|*.*";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    txtPathToObfuscate.Text = dlg.FileName;
                    UpdateOriginalText();
                }
            }
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            using (var settingsForm = new SettingsForm(_obfuscatorSettings))
            {
                if (settingsForm.ShowDialog() == DialogResult.OK)
                {
                    MessageBox.Show("Settings updated.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
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
            UpdateOriginalText();

            // Fyll _steps
            var originalCode = txtOriginal.Text;
            var tree = CSharpSyntaxTree.ParseText(originalCode);
            var root = tree.GetRoot();
            var obfuscator = new Obfuscator(tree, Log, _obfuscatorSettings);
            obfuscator.Visit(root);
            _steps = obfuscator.Steps;
            _currentStepIndex = 0;
            _currentTree = tree;

            Log($"Collected {_steps.Count} obfuscation steps.", "info");

            if (!chkSlowMode.Checked)
            {
                // Fast Mode: applicera alla steg direkt
                var finalTree = _steps.Aggregate(_currentTree, (t, step) => obfuscator.ApplyStep(t, step));
                var finalRoot = (CompilationUnitSyntax)finalTree.GetRoot();
                // Injicera decrypt-metod
                InjectDecryptMethodIntoRoot(ref finalRoot);
                var finalCode = finalRoot.NormalizeWhitespace().ToFullString();
                txtObfuscated.Text = finalCode;
                Log("Obfuscation completed successfully (fast mode).", "success");
            }
            else
            {
                // Slow Mode: steg-för-steg
                btnObfuscate.Enabled = false;
                btnNextStep.Enabled = true;
                lblStepInfo.Text = $"Step 0 / {_steps.Count} – click Next to begin.";
                txtObfuscated.Text = originalCode;
                HighlightSyntax(txtObfuscated);
                Log("Slow Mode: Ready to step through each transformation.", "info");
            }
        }

        private void btnNextStep_Click(object sender, EventArgs e)
        {
            if (_currentStepIndex >= _steps.Count)
            {
                // Alla steg klara -> injicera decrypt-metod om saknas
                var rootFinal = (CompilationUnitSyntax)_currentTree.GetRoot();
                var classNode = rootFinal.DescendantNodes()
                                         .OfType<ClassDeclarationSyntax>()
                                         .FirstOrDefault();
                if (classNode != null &&
                    !classNode.Members.OfType<MethodDeclarationSyntax>()
                               .Any(m => m.Identifier.Text == Obfuscator.DecryptMethodName))
                {
                    var tempObfuscator = new Obfuscator(_currentTree, Log, _obfuscatorSettings);
                    var decryptMethod = tempObfuscator.CreateDecryptMethod();
                    var newClassNode = classNode.AddMembers(decryptMethod);
                    rootFinal = rootFinal.ReplaceNode(classNode, newClassNode);
                    _currentTree = rootFinal.SyntaxTree;
                    txtObfuscated.Text = rootFinal.NormalizeWhitespace().ToFullString();
                    HighlightSyntax(txtObfuscated);
                }

                Log("All steps applied. Slow Mode complete.", "success");
                lblStepInfo.Text = $"Done: applied {_steps.Count} steps.";
                btnNextStep.Enabled = false;
                btnObfuscate.Enabled = true;
                return;
            }

            // Ta bort tidigare markering (om inte första steget)
            if (_currentStepIndex > 0)
            {
                var prev = _steps[_currentStepIndex - 1];
                txtObfuscated.Select(prev.Span.Start, prev.Span.Length);
                txtObfuscated.SelectionBackColor = txtObfuscated.BackColor;
                txtObfuscated.SelectionColor = Color.White;
            }

            // Visa beskrivning för aktuellt steg
            var step = _steps[_currentStepIndex];
            lblStepInfo.Text = $"Step {_currentStepIndex + 1}/{_steps.Count}: {step.Description}";
            Log($"[SlowMode] {step.Description}", "debug");

            // Applicera steget i AST-trädet
            var perStepObfuscator = new Obfuscator(_currentTree, Log, _obfuscatorSettings);
            _currentTree = perStepObfuscator.ApplyStep(_currentTree, step);

            // Uppdatera visning utan NormalizeWhitespace
            var newCode = _currentTree.GetRoot().ToFullString();
            txtObfuscated.Text = newCode;
            HighlightSyntax(txtObfuscated);

            // Markera det nya spannet
            txtObfuscated.Focus();
            txtObfuscated.Select(step.Span.Start, step.Span.Length);
            txtObfuscated.SelectionBackColor = Color.DarkBlue;
            txtObfuscated.SelectionColor = Color.White;
            txtObfuscated.ScrollToCaret();

            _currentStepIndex++;
            if (_currentStepIndex < _steps.Count)
            {
                btnNextStep.Text = "Next";
                lblStepInfo.Text = $"Step {_currentStepIndex}/{_steps.Count}: click Next for “{_steps[_currentStepIndex].Description}.”";
            }
            else
            {
                btnNextStep.Text = "Finish";
                lblStepInfo.Text = $"Step {_currentStepIndex}/{_steps.Count}: click to finish.";
            }
        }

        #endregion

        #region Hjälpmetoder

        private void InjectDecryptMethodIntoRoot(ref CompilationUnitSyntax root)
        {
            var classNode = root.DescendantNodes()
                                .OfType<ClassDeclarationSyntax>()
                                .FirstOrDefault();
            if (classNode != null)
            {
                var obfuscator = new Obfuscator(_currentTree, Log, _obfuscatorSettings);
                var decryptMethod = obfuscator.CreateDecryptMethod();
                var newClassNode = classNode.AddMembers(decryptMethod);
                root = root.ReplaceNode(classNode, newClassNode);
            }
        }

        public void Log(string message, string level = "info")
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action(() => Log(message, level)));
                return;
            }

            string time = DateTime.Now.ToString("HH:mm:ss");
            string formattedMessage = $"[{time}] {message}";

            Color logColor = level.ToLower() switch
            {
                "info" => Color.Blue,
                "warning" => Color.Orange,
                "error" => Color.Red,
                "success" => Color.Green,
                "debug" => Color.White,
                _ => Color.White
            };

            AppendTextWithColor(txtLog, formattedMessage + Environment.NewLine, logColor);
        }

        private void AppendTextWithColor(RichTextBox box, string text, Color color)
        {
            box.SelectionStart = box.TextLength;
            box.SelectionLength = 0;
            box.SelectionColor = color;
            box.AppendText(text);
            box.SelectionColor = box.ForeColor;
        }

        private void UpdateOriginalText()
        {
            if (!File.Exists(txtPathToObfuscate.Text))
                return;

            string code = File.ReadAllText(txtPathToObfuscate.Text);
            txtOriginal.Text = code;
            HighlightSyntax(txtOriginal);
        }

        #endregion

        #region Syntaxhighlighting

        private void HighlightSyntax(RichTextBox rtb)
        {
            int originalSelStart = rtb.SelectionStart;
            int originalSelLength = rtb.SelectionLength;

            rtb.SuspendLayout();

            Color defaultColor = Color.White;
            Font defaultFont = new Font("Consolas", 9F, FontStyle.Regular);

            rtb.SelectAll();
            rtb.SelectionColor = defaultColor;
            rtb.SelectionFont = defaultFont;

            string allText = rtb.Text;

            var commentBlockPattern = new Regex(@"/\*.*?\*/", RegexOptions.Singleline);
            var commentLinePattern = new Regex(@"//.*?$", RegexOptions.Multiline);
            var stringPattern = new Regex(@""".*?(?<!\\)""|@""[^""]*""", RegexOptions.Singleline);

            string[] keywords = new[]
            {
                "abstract","as","base","bool","break","byte","case","catch","char","checked",
                "class","const","continue","decimal","default","delegate","do","double","else",
                "enum","event","explicit","extern","false","finally","fixed","float","for",
                "foreach","goto","if","implicit","in","int","interface","internal","is","lock",
                "long","namespace","new","null","object","operator","out","override","params",
                "private","protected","public","readonly","ref","return","sbyte","sealed",
                "short","sizeof","stackalloc","static","string","struct","switch","this",
                "throw","true","try","typeof","uint","ulong","unchecked","unsafe","ushort",
                "using","virtual","void","volatile","while"
            };
            var regexKeywords = new Regex(@"\b(" + string.Join("|", keywords) + @")\b");

            foreach (Match m in commentBlockPattern.Matches(allText))
            {
                rtb.Select(m.Index, m.Length);
                rtb.SelectionColor = Color.Green;
                rtb.SelectionFont = new Font("Consolas", 9F, FontStyle.Italic);
            }

            foreach (Match m in commentLinePattern.Matches(allText))
            {
                rtb.Select(m.Index, m.Length);
                rtb.SelectionColor = Color.Green;
                rtb.SelectionFont = new Font("Consolas", 9F, FontStyle.Italic);
            }

            foreach (Match m in stringPattern.Matches(allText))
            {
                rtb.Select(m.Index, m.Length);
                rtb.SelectionColor = Color.Brown;
                rtb.SelectionFont = new Font("Consolas", 9F, FontStyle.Regular);
            }

            foreach (Match m in regexKeywords.Matches(allText))
            {
                rtb.Select(m.Index, m.Length);
                if (rtb.SelectionColor == defaultColor)
                {
                    rtb.SelectionColor = Color.Blue;
                    rtb.SelectionFont = new Font("Consolas", 9F, FontStyle.Bold);
                }
            }

            rtb.Select(originalSelStart, originalSelLength);
            rtb.SelectionColor = defaultColor;

            rtb.ResumeLayout();
        }

        #endregion
    }

    #region Obfuscator-klass

    class Obfuscator : CSharpSyntaxRewriter
    {
        #region Fält & Konstruktor

        public List<ObfuscationStep> Steps { get; } = new List<ObfuscationStep>();
        private readonly SemanticModel _semanticModel;
        private readonly Dictionary<string, string> _nameMap = new Dictionary<string, string>();
        private readonly Stack<Dictionary<string, string>> _localScopeStack = new Stack<Dictionary<string, string>>();
        private readonly Random _random = new Random();
        private readonly Action<string, string> _log;
        private readonly ObfuscatorSettings _settings;
        private bool _decryptStringInjected = false;

        public static string DecryptMethodName => "M_mgbkf";

        private readonly string _decryptStringVariableName = "V_homft";
        private readonly string _decryptStringVariable2Name = "V_xkgpd";

        private static readonly HashSet<string> SystemIdentifiers = new HashSet<string>
        {
            "System", "Console", "Microsoft", "Convert", "Encoding", "Environment",
            "DateTime", "Exception", "Task", "List", "Enumerable", "ToUpperInvariant",
            "Trim", "ToUpper", "Substring", "ToLower", "Where", "ToList", "Join",
            "Parse", "Length"
        };

        public Obfuscator(SyntaxTree originalTree, Action<string, string> logAction, ObfuscatorSettings settings = null)
        {
            _log = logAction;
            _settings = settings ?? new ObfuscatorSettings();

            var compilation = CSharpCompilation.Create(
                "Temp",
                syntaxTrees: new[] { originalTree },
                references: new[]
                {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(TcpClient).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.Net.IPAddress).Assembly.Location),
                    MetadataReference.CreateFromFile(
                        Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location), "System.Runtime.dll")),
                    MetadataReference.CreateFromFile(
                        Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location), "netstandard.dll"))
                },
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );

            _semanticModel = compilation.GetSemanticModel(originalTree);
        }

        #endregion

        #region Hjälpfunktioner för namn

        private bool IsMainMethod(MethodDeclarationSyntax method)
        {
            bool isVoidMain = method.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)) &&
                              method.ReturnType is PredefinedTypeSyntax pts &&
                              pts.Keyword.IsKind(SyntaxKind.VoidKeyword);

            bool isAsyncTaskMain = method.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)) &&
                                   method.Modifiers.Any(m => m.IsKind(SyntaxKind.AsyncKeyword)) &&
                                   method.ReturnType is IdentifierNameSyntax idName &&
                                   idName.Identifier.Text == "Task";

            return isVoidMain || isAsyncTaskMain;
        }

        private string GenerateRandomName()
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var generated = new string(
                Enumerable.Repeat(chars, 6)
                          .Select(s => s[_random.Next(s.Length)])
                          .ToArray()
            );
            _log($"[GenerateRandomName] -> {generated}", "debug");
            return generated;
        }

        private void AddGlobalMapping(string originalName, string prefix)
        {
            if (!_nameMap.ContainsKey(originalName))
            {
                string newName = prefix + "_" + GenerateRandomName();
                _nameMap[originalName] = newName;
                _log($"[AddGlobalMapping] '{originalName}' -> '{newName}'", "success");
            }
            else
            {
                _log($"[AddGlobalMapping] already exists: '{originalName}' -> '{_nameMap[originalName]}'", "info");
            }
        }

        private string LookupGlobalMapping(string originalName)
        {
            if (_nameMap.TryGetValue(originalName, out var newName))
            {
                _log($"[LookupGlobalMapping] Found mapping for '{originalName}' -> '{newName}'", "debug");
                return newName;
            }
            else
            {
                _log($"[LookupGlobalMapping] WARNING: No mapping found for '{originalName}'", "warning");
                return null;
            }
        }

        private void PushLocalScope()
        {
            _localScopeStack.Push(new Dictionary<string, string>());
        }

        private void PopLocalScope()
        {
            _localScopeStack.Pop();
        }

        private void AddLocalMapping(string originalName, string newName)
        {
            if (_localScopeStack.Count > 0)
            {
                _localScopeStack.Peek()[originalName] = newName;
                _log($"[AddLocalMapping] '{originalName}' -> '{newName}'", "debug");
            }
        }

        private string LookupLocalMapping(string originalName)
        {
            foreach (var scope in _localScopeStack)
            {
                if (scope.TryGetValue(originalName, out var newName))
                {
                    _log($"[LookupLocalMapping] Found mapping for '{originalName}' -> '{newName}'", "debug");
                    return newName;
                }
            }
            return null;
        }

        #endregion

        #region Överskrivningar för lokala scopes

        public override SyntaxNode VisitBlock(BlockSyntax node)
        {
            PushLocalScope();
            var visitedBlock = (BlockSyntax)base.VisitBlock(node);
            PopLocalScope();
            return visitedBlock;
        }

        public override SyntaxNode VisitParameter(ParameterSyntax node)
        {
            string originalName = node.Identifier.Text;
            string newName = LookupLocalMapping(originalName);
            if (newName == null)
            {
                newName = "P_" + GenerateRandomName();
                AddLocalMapping(originalName, newName);

                Steps.Add(new ObfuscationStep
                {
                    Description = $"Renaming parameter '{originalName}' → '{newName}'",
                    Span = node.Identifier.Span,
                    BeforeText = originalName,
                    AfterText = newName,
                    VariableSnapshot = null
                });
            }

            return base.VisitParameter(node);
        }

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

                    Steps.Add(new ObfuscationStep
                    {
                        Description = $"Renaming field '{originalName}' → '{newName}'",
                        Span = node.Identifier.Span,
                        BeforeText = originalName,
                        AfterText = newName,
                        VariableSnapshot = null
                    });
                }
            }
            else
            {
                newName = LookupLocalMapping(originalName);
                if (newName == null)
                {
                    newName = "V_" + GenerateRandomName();
                    AddLocalMapping(originalName, newName);

                    Steps.Add(new ObfuscationStep
                    {
                        Description = $"Renaming variable '{originalName}' → '{newName}'",
                        Span = node.Identifier.Span,
                        BeforeText = originalName,
                        AfterText = newName,
                        VariableSnapshot = null
                    });
                }
            }

            var visited = (VariableDeclaratorSyntax)base.VisitVariableDeclarator(node);

            if (!isField && visited.Initializer == null)
            {
                if (node.Parent is VariableDeclarationSyntax varDecl &&
                    varDecl.Type is TypeSyntax declaredType)
                {
                    ExpressionSyntax defaultExpr = null;

                    if (declaredType is PredefinedTypeSyntax pts)
                    {
                        switch (pts.Keyword.Kind())
                        {
                            case SyntaxKind.IntKeyword:
                            case SyntaxKind.LongKeyword:
                            case SyntaxKind.ShortKeyword:
                            case SyntaxKind.ByteKeyword:
                            case SyntaxKind.UIntKeyword:
                            case SyntaxKind.ULongKeyword:
                            case SyntaxKind.UShortKeyword:
                            case SyntaxKind.SByteKeyword:
                                defaultExpr = SyntaxFactory.LiteralExpression(
                                    SyntaxKind.NumericLiteralExpression,
                                    SyntaxFactory.Literal(0)
                                );
                                break;
                            case SyntaxKind.DoubleKeyword:
                            case SyntaxKind.FloatKeyword:
                            case SyntaxKind.DecimalKeyword:
                                defaultExpr = SyntaxFactory.LiteralExpression(
                                    SyntaxKind.NumericLiteralExpression,
                                    SyntaxFactory.Literal(0.0)
                                );
                                break;
                            case SyntaxKind.CharKeyword:
                                defaultExpr = SyntaxFactory.LiteralExpression(
                                    SyntaxKind.CharacterLiteralExpression,
                                    SyntaxFactory.Literal('\0')
                                );
                                break;
                            case SyntaxKind.BoolKeyword:
                                defaultExpr = SyntaxFactory.LiteralExpression(
                                    SyntaxKind.FalseLiteralExpression
                                );
                                break;
                            case SyntaxKind.StringKeyword:
                                defaultExpr = null;
                                break;
                            default:
                                defaultExpr = SyntaxFactory.DefaultExpression(declaredType);
                                break;
                        }
                    }
                    else
                    {
                        defaultExpr = SyntaxFactory.DefaultExpression(declaredType);
                    }

                    if (defaultExpr != null)
                    {
                        var defaultInit = SyntaxFactory.EqualsValueClause(defaultExpr);
                        visited = visited.WithInitializer(defaultInit);
                    }
                }
            }

            visited = visited.WithIdentifier(SyntaxFactory.Identifier(newName));
            return visited;
        }

        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
        {
            string original = node.Identifier.Text;
            string newName = LookupLocalMapping(original) ?? LookupGlobalMapping(original);
            if (newName != null && original != newName)
            {
                Steps.Add(new ObfuscationStep
                {
                    Description = $"Replacing usage '{original}' → '{newName}'",
                    Span = node.Identifier.Span,
                    BeforeText = original,
                    AfterText = newName,
                    VariableSnapshot = null
                });
            }
            return base.VisitIdentifierName(node);
        }

        public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (node.Expression is IdentifierNameSyntax identifier)
            {
                string originalName = identifier.Identifier.Text;
                string newName = LookupLocalMapping(originalName) ?? LookupGlobalMapping(originalName);
                if (newName != null && originalName != newName)
                {
                    Steps.Add(new ObfuscationStep
                    {
                        Description = $"Replacing invocation '{originalName}' → '{newName}'",
                        Span = identifier.Span,
                        BeforeText = originalName,
                        AfterText = newName,
                        VariableSnapshot = null
                    });
                }
            }
            return base.VisitInvocationExpression(node);
        }

        #endregion

        #region ApplyStep

        public SyntaxTree ApplyStep(SyntaxTree currentTree, ObfuscationStep step)
        {
            var root = (CompilationUnitSyntax)currentTree.GetRoot();
            var token = root.FindToken(step.Span.Start);

            bool isConcatExpr = step.AfterText.Contains("+") || step.AfterText.Contains("(");

            if (isConcatExpr)
            {
                var literalNode = root.DescendantNodes()
                                      .OfType<LiteralExpressionSyntax>()
                                      .FirstOrDefault(l => l.Token.Span == step.Span);

                if (literalNode == null)
                {
                    literalNode = root.DescendantNodes()
                                      .OfType<LiteralExpressionSyntax>()
                                      .FirstOrDefault(l => l.GetText().ToString() == step.BeforeText);
                }

                if (literalNode == null)
                {
                    _log($"[Warning] Could not locate literal node '{step.BeforeText}' for replacement; skipping.", "warn");
                    return currentTree;
                }

                ExpressionSyntax newExpr;
                try
                {
                    newExpr = SyntaxFactory.ParseExpression(step.AfterText);
                }
                catch
                {
                    _log($"[Warning] Failed to parse concatenation expression: {step.AfterText}; skipping.", "warn");
                    return currentTree;
                }

                newExpr = newExpr
                          .WithLeadingTrivia(literalNode.GetLeadingTrivia())
                          .WithTrailingTrivia(literalNode.GetTrailingTrivia());

                var newRoot = root.ReplaceNode(literalNode, newExpr);
                return CSharpSyntaxTree.Create(
                    newRoot,
                    (CSharpParseOptions)currentTree.Options,
                    currentTree.FilePath,
                    ((CSharpSyntaxTree)currentTree).Encoding
                );
            }
            else
            {
                bool matchesBySpan = token.ValueText == step.BeforeText.Trim('"') ||
                                     token.ToString() == step.BeforeText;

                SyntaxToken tokenToReplace = default;
                bool isStringLiteral = step.BeforeText.StartsWith("\"") && step.BeforeText.EndsWith("\"");

                if (matchesBySpan)
                {
                    if ((token.IsKind(SyntaxKind.IdentifierToken) && !isStringLiteral) ||
                        (isStringLiteral && token.IsKind(SyntaxKind.StringLiteralToken)))
                    {
                        tokenToReplace = token;
                    }
                }

                if (tokenToReplace == default)
                {
                    foreach (var t in root.DescendantTokens())
                    {
                        if (!isStringLiteral)
                        {
                            if (t.IsKind(SyntaxKind.IdentifierToken) && t.ValueText == step.BeforeText)
                            {
                                tokenToReplace = t;
                                break;
                            }
                        }
                        else
                        {
                            if (t.IsKind(SyntaxKind.StringLiteralToken) && t.ToString() == step.BeforeText)
                            {
                                tokenToReplace = t;
                                break;
                            }
                        }
                    }
                }

                if (tokenToReplace == default)
                {
                    _log($"[Warning] Could not locate token '{step.BeforeText}' for replacement; skipping.", "warn");
                    return currentTree;
                }

                SyntaxToken newToken;
                if (tokenToReplace.IsKind(SyntaxKind.IdentifierToken))
                {
                    newToken = SyntaxFactory.Identifier(step.AfterText)
                                             .WithLeadingTrivia(tokenToReplace.LeadingTrivia)
                                             .WithTrailingTrivia(tokenToReplace.TrailingTrivia);
                }
                else if (tokenToReplace.IsKind(SyntaxKind.StringLiteralToken))
                {
                    newToken = SyntaxFactory.Literal(step.AfterText.Trim('"'))
                                           .WithLeadingTrivia(tokenToReplace.LeadingTrivia)
                                           .WithTrailingTrivia(tokenToReplace.TrailingTrivia);
                }
                else
                {
                    _log($"[Warning] Token kind {tokenToReplace.Kind()} not supported for replacement; skipping.", "warn");
                    return currentTree;
                }

                var newRoot = root.ReplaceToken(tokenToReplace, newToken);
                return CSharpSyntaxTree.Create(
                    newRoot,
                    (CSharpParseOptions)currentTree.Options,
                    currentTree.FilePath,
                    ((CSharpSyntaxTree)currentTree).Encoding
                );
            }
        }

        #endregion

        #region Överskrivningar för global mapping

        public override SyntaxNode VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            if (node.Modifiers.Any(tok => tok.IsKind(SyntaxKind.ConstKeyword)))
            {
                var newModifiers = new SyntaxTokenList(
                    node.Modifiers.Where(tok => !tok.IsKind(SyntaxKind.ConstKeyword)).ToArray()
                );

                var newNode = node.WithModifiers(newModifiers);
                return base.VisitLocalDeclarationStatement(newNode);
            }

            return base.VisitLocalDeclarationStatement(node);
        }

        public override SyntaxNode VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            if (node.IsKind(SyntaxKind.StringLiteralExpression))
            {
                if (node.Parent is CaseSwitchLabelSyntax)
                    return node;

                string originalString = node.Token.ValueText;
                ExpressionSyntax replacementExpr;

                if (!_settings.EnableStringSplitting || originalString.Length <= 4)
                {
                    string encrypted = Convert.ToBase64String(Encoding.UTF8.GetBytes(originalString));
                    _log($"[VisitLiteralExpression] Encrypting '{originalString}' → {DecryptMethodName}(\"{encrypted}\")", "info");

                    Steps.Add(new ObfuscationStep
                    {
                        Description = $"Encrypting literal '{originalString}' → {DecryptMethodName}(\"{encrypted}\")",
                        Span = node.Token.Span,
                        BeforeText = $"\"{originalString}\"",
                        AfterText = $"{DecryptMethodName}(\"{encrypted}\")",
                        VariableSnapshot = null
                    });

                    replacementExpr = SyntaxFactory.InvocationExpression(
                        SyntaxFactory.IdentifierName(DecryptMethodName),
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Argument(
                                    SyntaxFactory.LiteralExpression(
                                        SyntaxKind.StringLiteralExpression,
                                        SyntaxFactory.Literal(encrypted)
                                    )
                                )
                            )
                        )
                    );
                }
                else
                {
                    _log($"[VisitLiteralExpression] Splitting and encrypting '{originalString}'", "info");
                    int pieces = Math.Min(5, originalString.Length);
                    pieces = _random.Next(2, pieces + 1);
                    int pieceLen = originalString.Length / pieces;
                    var splitPieces = new List<string>();
                    int idx = 0;
                    for (int i = 0; i < pieces; i++)
                    {
                        if (i == pieces - 1)
                        {
                            splitPieces.Add(originalString.Substring(idx));
                        }
                        else
                        {
                            splitPieces.Add(originalString.Substring(idx, pieceLen));
                            idx += pieceLen;
                        }
                    }

                    var partExprs = new List<ExpressionSyntax>();
                    foreach (var piece in splitPieces)
                    {
                        string encPart = Convert.ToBase64String(Encoding.UTF8.GetBytes(piece));
                        var call = SyntaxFactory.InvocationExpression(
                            SyntaxFactory.IdentifierName(DecryptMethodName),
                            SyntaxFactory.ArgumentList(
                                SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory.Argument(
                                        SyntaxFactory.LiteralExpression(
                                            SyntaxKind.StringLiteralExpression,
                                            SyntaxFactory.Literal(encPart)
                                        )
                                    )
                                )
                            )
                        );
                        partExprs.Add(call);
                    }

                    string after = string.Join(" + ",
                        splitPieces
                          .Select(p => $"{DecryptMethodName}(\"{Convert.ToBase64String(Encoding.UTF8.GetBytes(p))}\")")
                    );

                    Steps.Add(new ObfuscationStep
                    {
                        Description = $"Splitting/encrypting '{originalString}' into {pieces} parts",
                        Span = node.Token.Span,
                        BeforeText = $"\"{originalString}\"",
                        AfterText = after,
                        VariableSnapshot = null
                    });

                    ExpressionSyntax concatenated = partExprs[0];
                    for (int i = 1; i < partExprs.Count; i++)
                    {
                        concatenated = SyntaxFactory.BinaryExpression(
                            SyntaxKind.AddExpression,
                            concatenated,
                            partExprs[i]
                        );
                    }

                    replacementExpr = concatenated;
                }

                return replacementExpr;
            }

            return base.VisitLiteralExpression(node);
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            string originalClassName = node.Identifier.Text;
            if (IsSystemType(originalClassName))
                return base.VisitClassDeclaration(node);

            string newName = LookupGlobalMapping(originalClassName);
            if (newName == null)
            {
                newName = "C_" + GenerateRandomName();
                AddGlobalMapping(originalClassName, "C");
                newName = LookupGlobalMapping(originalClassName);
            }

            Steps.Add(new ObfuscationStep
            {
                Description = $"Renaming class '{originalClassName}' → '{newName}'",
                Span = node.Identifier.Span,
                BeforeText = node.Identifier.Text,
                AfterText = newName,
                VariableSnapshot = null
            });

            return base.VisitClassDeclaration(node);
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            string originalName = node.Identifier.Text;

            if (node.ExplicitInterfaceSpecifier != null ||
                node.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)))
            {
                _log($"[SkipMethod] Preserving {originalName}", "info");
                return base.VisitMethodDeclaration(node);
            }

            if (originalName == "Main" && IsMainMethod(node))
                return base.VisitMethodDeclaration(node);

            string newName = LookupGlobalMapping(originalName);
            if (newName == null)
            {
                newName = "M_" + GenerateRandomName();
                AddGlobalMapping(originalName, "M");
                newName = LookupGlobalMapping(originalName);
            }

            Steps.Add(new ObfuscationStep
            {
                Description = $"Renaming method '{originalName}' → '{newName}'",
                Span = node.Identifier.Span,
                BeforeText = originalName,
                AfterText = newName,
                VariableSnapshot = null
            });

            PushLocalScope();

            var visitedParams = (ParameterListSyntax)Visit(node.ParameterList);
            node = node.WithParameterList(visitedParams);

            if (node.Body != null)
            {
                var newBody = (BlockSyntax)Visit(node.Body);
                node = node.WithBody(newBody);
            }

            PopLocalScope();

            return node;
        }

        public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            if (node.Modifiers.Any(tok => tok.IsKind(SyntaxKind.ConstKeyword)))
            {
                var newModifiers = new SyntaxTokenList(
                    node.Modifiers.Where(tok => !tok.IsKind(SyntaxKind.ConstKeyword)).ToArray()
                );

                int insertPos = 0;
                if (newModifiers.Count > 0)
                {
                    var first = newModifiers.First();
                    if (first.IsKind(SyntaxKind.PublicKeyword) ||
                        first.IsKind(SyntaxKind.PrivateKeyword) ||
                        first.IsKind(SyntaxKind.InternalKeyword) ||
                        first.IsKind(SyntaxKind.ProtectedKeyword))
                    {
                        insertPos = 1;
                    }
                }

                newModifiers = newModifiers.Insert(insertPos, SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword));
                newModifiers = newModifiers.Insert(insertPos, SyntaxFactory.Token(SyntaxKind.StaticKeyword));

                var newNode = node.WithModifiers(newModifiers);
                return base.VisitFieldDeclaration(newNode);
            }

            return base.VisitFieldDeclaration(node);
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

        #region Övrigt (Switch-flattening, DecryptMethod)

        public override SyntaxNode VisitSwitchStatement(SwitchStatementSyntax node)
        {
            _log($"[VisitSwitchStatement] Obfuscating switch statement.", "info");

            var entireSwitchSpan = node.SwitchKeyword.Span;
            Steps.Add(new ObfuscationStep
            {
                Description = $"Flattening switch at line {node.GetLocation().GetLineSpan().StartLinePosition.Line + 1}",
                Span = entireSwitchSpan,
                BeforeText = node.ToString().Split(Environment.NewLine)[0] + "...",
                AfterText = "/* flattened state‐machine */",
                VariableSnapshot = null
            });

            var stateVarName = "V_" + GenerateRandomName();
            AddLocalMapping(stateVarName, stateVarName);

            var visitedExpr = (ExpressionSyntax)Visit(node.Expression);

            bool isStringSwitch = node.Sections
                .SelectMany(sec => sec.Labels)
                .OfType<CaseSwitchLabelSyntax>()
                .Any(lbl => lbl.Value is LiteralExpressionSyntax lit &&
                            lit.IsKind(SyntaxKind.StringLiteralExpression));

            bool isCharSwitch = node.Sections
                .SelectMany(sec => sec.Labels)
                .OfType<CaseSwitchLabelSyntax>()
                .Any(lbl => lbl.Value is LiteralExpressionSyntax lit2 &&
                            lit2.IsKind(SyntaxKind.CharacterLiteralExpression));

            if (isStringSwitch || isCharSwitch)
                return base.VisitSwitchStatement(node);

            var rawSections = node.Sections
                .Select((sec, idx) => new { Section = sec, Index = idx })
                .Where(x => !x.Section.Labels.OfType<DefaultSwitchLabelSyntax>().Any())
                .OrderBy(x => _random.Next())
                .ToList();

            var caseMappings = new Dictionary<int, int>();
            foreach (var entry in rawSections)
            {
                caseMappings[entry.Index] = _random.Next(1000, 9999);
            }

            IfStatementSyntax ifChain = null;
            IfStatementSyntax currentIf = null;
            bool firstIf = true;

            foreach (var entry in rawSections)
            {
                var intLabel = entry.Section.Labels
                    .OfType<CaseSwitchLabelSyntax>()
                    .Select(lbl => lbl.Value)
                    .OfType<LiteralExpressionSyntax>()
                    .FirstOrDefault(lit => lit.IsKind(SyntaxKind.NumericLiteralExpression));

                if (intLabel == null) continue;

                int literalInt = (int)intLabel.Token.Value;
                int thisKey = caseMappings[entry.Index];

                var cmp = SyntaxFactory.BinaryExpression(
                    SyntaxKind.EqualsExpression,
                    visitedExpr,
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal(literalInt)
                    )
                );

                var assignStmt = SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.IdentifierName(stateVarName),
                        SyntaxFactory.LiteralExpression(
                            SyntaxKind.NumericLiteralExpression,
                            SyntaxFactory.Literal(thisKey)
                        )
                    )
                );

                var ifStmt = SyntaxFactory.IfStatement(cmp, assignStmt);

                if (firstIf)
                {
                    ifChain = ifStmt;
                    currentIf = ifChain;
                    firstIf = false;
                }
                else
                {
                    currentIf = currentIf.WithElse(SyntaxFactory.ElseClause(ifStmt));
                    currentIf = ifStmt;
                }
            }

            StatementSyntax assignChainOrInit = ifChain;

            var flattenedSections = new List<SwitchSectionSyntax>();
            int count = rawSections.Count;

            for (int i = 0; i < count; i++)
            {
                var entry = rawSections[i];
                int thisKey = caseMappings[entry.Index];
                bool isLast = (i == count - 1);
                int nextKey = isLast ? -1 : caseMappings[rawSections[i + 1].Index];

                var visitedStmts = entry.Section.Statements
                    .Select(s => (StatementSyntax)Visit(s))
                    .ToList();

                visitedStmts.Add(
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            SyntaxFactory.IdentifierName(stateVarName),
                            SyntaxFactory.LiteralExpression(
                                SyntaxKind.NumericLiteralExpression,
                                SyntaxFactory.Literal(nextKey)
                            )
                        )
                    )
                );
                visitedStmts.Add(SyntaxFactory.BreakStatement());

                var caseLabel = SyntaxFactory.CaseSwitchLabel(
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal(thisKey)
                    )
                );
                var switchSection = SyntaxFactory.SwitchSection(
                    SyntaxFactory.SingletonList<SwitchLabelSyntax>(caseLabel),
                    SyntaxFactory.List(visitedStmts)
                );
                flattenedSections.Add(switchSection);
            }

            var defaultSectionEnd = SyntaxFactory.SwitchSection(
                SyntaxFactory.SingletonList<SwitchLabelSyntax>(SyntaxFactory.DefaultSwitchLabel()),
                SyntaxFactory.SingletonList<StatementSyntax>(SyntaxFactory.BreakStatement())
            );
            flattenedSections.Add(defaultSectionEnd);

            var switchStmt = SyntaxFactory.SwitchStatement(
                SyntaxFactory.IdentifierName(stateVarName),
                SyntaxFactory.List(flattenedSections)
            );
            var whileLoop = SyntaxFactory.WhileStatement(
                SyntaxFactory.BinaryExpression(
                    SyntaxKind.NotEqualsExpression,
                    SyntaxFactory.IdentifierName(stateVarName),
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal(-1)
                    )
                ),
                SyntaxFactory.Block(switchStmt)
            );

            if (assignChainOrInit != null)
            {
                return SyntaxFactory.Block(assignChainOrInit, whileLoop);
            }
            else
            {
                return SyntaxFactory.Block(whileLoop);
            }
        }

        private bool IsSystemType(string identifier) =>
            SystemIdentifiers.Contains(identifier) ||
            identifier.StartsWith("System.") ||
            identifier.StartsWith("Microsoft.");

        public MethodDeclarationSyntax CreateDecryptMethod() =>
            SyntaxFactory.ParseMemberDeclaration($@"
                private static string {DecryptMethodName}(string {_decryptStringVariableName})
                {{
                    if (string.IsNullOrEmpty({_decryptStringVariableName})) return string.Empty;
                    byte[] {_decryptStringVariable2Name} = Convert.FromBase64String({_decryptStringVariableName});
                    return System.Text.Encoding.UTF8.GetString({_decryptStringVariable2Name});
                }}") as MethodDeclarationSyntax;

        #endregion

        #region Ytterligare Hjälpfunktioner

        private void AddNameMap(string originalName, string prefix)
        {
            if (!_nameMap.ContainsKey(originalName))
            {
                string newName = prefix + "_" + GenerateRandomName();
                _nameMap[originalName] = newName;
                _log($"[AddNameMap] '{originalName}' -> '{newName}'", "success");
            }
            else
            {
                _log($"[AddNameMap] already exists: '{originalName}' -> '{_nameMap[originalName]}'", "info");
            }
        }

        private string LookupName(string originalName)
        {
            if (_nameMap.TryGetValue(originalName, out var newName))
            {
                _log($"[LookupName] Found mapping for '{originalName}' -> '{newName}'", "debug");
                return newName;
            }
            else
            {
                _log($"[LookupName] WARNING: No mapping found for '{originalName}'", "warning");
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
            int randomValue = _random.Next(0, 100);
            string randomString = GenerateRandomName();
            var dummyDeclaration = SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)))
                .WithVariables(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(
                            SyntaxFactory.Identifier("V_" + randomString))
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
            ).NormalizeWhitespace();

            var block = SyntaxFactory.Block(dummyDeclaration);
            return SyntaxFactory.IfStatement(condition, block);
        }

        #endregion
    }

    #endregion

    public class ObfuscationStep
    {
        public string Description { get; set; }
        public TextSpan Span { get; set; }
        public string BeforeText { get; set; }
        public string AfterText { get; set; }
        public string VariableSnapshot { get; set; }
    }

}
