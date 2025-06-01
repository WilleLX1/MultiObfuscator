using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualBasic.Logging;
using System.Text.RegularExpressions;
using System.Net.Sockets;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using System.IO;                // ←─ Added for Path, File, etc.
using System.Windows.Forms;    // ←─ Make sure WinForms references are available
using System.Drawing;          // ←─ For Color

namespace MultiObfuscator
{
    public partial class Form1 : Form
    {
        // Keep track of the step list and current index:
        private List<ObfuscationStep> _steps;
        private int _currentStepIndex;
        private SyntaxTree _currentTree; // holds the tree as we apply steps one by one
        // Hold the settings instance.
        private ObfuscatorSettings obfuscatorSettings = new ObfuscatorSettings();

        public Form1()
        {
            InitializeComponent();
            chkSlowMode.CheckedChanged += (s, e) => btnNextStep.Enabled = chkSlowMode.Checked;
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
                    MessageBox.Show($"Syntax errors detected in original file:\n{string.Join("\n", errors)}",
                                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var obfuscator = new Obfuscator(tree, Log, obfuscatorSettings);

                // 1) Run the rewriter to gather all steps and produce a new root
                var visitedRoot = obfuscator.Visit(root);

                // ―――――――――――――――――――――――――――――――――――――
                // NEW SECTION: inject the decrypt‐method into the primary class
                // ―――――――――――――――――――――――――――――――――――――
                {
                    var classNode = visitedRoot
                                    .DescendantNodes()
                                    .OfType<ClassDeclarationSyntax>()
                                    .FirstOrDefault();
                    if (classNode != null)
                    {
                        // Create a new MethodDeclarationSyntax for M_mgbkf(...)
                        // (CreateDecryptMethod() is now public.)
                        var decryptMethod = obfuscator.CreateDecryptMethod();

                        // Append it to the existing class
                        var newClassNode = classNode.AddMembers(decryptMethod);

                        // Replace the old class node with the new one
                        visitedRoot = visitedRoot.ReplaceNode(classNode, newClassNode);
                    }
                }
                // ―――――――――――――――――――――――――――――――――――――

                // Normalize whitespace now that we've inserted M_mgbkf
                var obfuscatedRoot = visitedRoot.NormalizeWhitespace();
                Log("Obfuscation completed successfully.", "success");

                // --- NEW SECTION: Try compiling the obfuscated code in-memory to catch errors. ---
                string obfCodeText = obfuscatedRoot.ToFullString();
                SyntaxTree obfTree = CSharpSyntaxTree.ParseText(obfCodeText);

                // Build a compilation that references mscorlib, System.Console, etc.
                var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);
                var references = new[]
                {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),

                    // Add both Sockets and Primitives assemblies:
                    MetadataReference.CreateFromFile(typeof(TcpClient).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.Net.IPAddress).Assembly.Location),

                    MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Runtime.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "netstandard.dll"))
                };

                var compilation = CSharpCompilation.Create(
                    assemblyName: "ObfuscatedCheck",
                    syntaxTrees: new[] { obfTree },
                    references: references,
                    options: new CSharpCompilationOptions(OutputKind.ConsoleApplication)
                );

                // Get diagnostics (errors/warnings) from the obfuscated code
                var obfDiagnostics = compilation.GetDiagnostics()
                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                    .ToList();

                if (obfDiagnostics.Any())
                {
                    Log("Compilation errors detected in obfuscated code:", "error");
                    foreach (var diag in obfDiagnostics)
                    {
                        Log($"  {diag}", "error");
                    }
                    MessageBox.Show(
                        "Obfuscated code has compilation errors. Check the log for details.",
                        "Obfuscation Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    // Still write the obfuscated code to the textbox so you can inspect it:
                    txtObfuscated.Text = obfCodeText;
                    return;
                }
                else
                {
                    Log("Obfuscated code compiled successfully (no errors).", "success");
                }
                // --- END NEW SECTION ---

                // Finally, write the obfuscated code into the text box
                txtObfuscated.Text = obfCodeText;
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
            UpdateOriginalText();

            // Phase A: build the step list
            var originalCode = txtOriginal.Text;
            var tree = CSharpSyntaxTree.ParseText(originalCode);
            var root = tree.GetRoot();

            var obfuscator = new Obfuscator(tree, Log, obfuscatorSettings);
            obfuscator.Visit(root);                    // Populate obfuscator.Steps
            _steps = obfuscator.Steps;                 // List<ObfuscationStep>
            _currentStepIndex = 0;
            _currentTree = tree;                       // start with the original AST

            Log($"Collected {_steps.Count} obfuscation steps.", "info");

            if (!chkSlowMode.Checked)
            {
                // Fast Mode: just apply them all in one go:
                var finalTree = _steps.Aggregate(_currentTree, (t, step) => obfuscator.ApplyStep(t, step));
                var finalRoot = (CompilationUnitSyntax)finalTree.GetRoot();

                // ―――――――――――――――――――――――――――――――――――――
                // NEW SECTION: after applying all token‐based steps, inject M_mgbkf(...)
                {
                    var classNode = finalRoot
                                    .DescendantNodes()
                                    .OfType<ClassDeclarationSyntax>()
                                    .FirstOrDefault();
                    if (classNode != null)
                    {
                        var decryptMethod = obfuscator.CreateDecryptMethod();
                        var newClassNode = classNode.AddMembers(decryptMethod);
                        finalRoot = finalRoot.ReplaceNode(classNode, newClassNode);
                    }
                }
                // ―――――――――――――――――――――――――――――――――――――

                var finalCode = finalRoot.NormalizeWhitespace().ToFullString();
                txtObfuscated.Text = finalCode;
                Log("Obfuscation completed successfully (fast mode).", "success");
            }
            else
            {
                // Slow Mode: disable btnObfuscate to prevent re‐entry, enable Next
                btnObfuscate.Enabled = false;
                btnNextStep.Enabled = true;
                lblStepInfo.Text = $"Step 0 / {_steps.Count} – click Next to begin.";
                // Show the original code in txtObfuscated, ready to be transformed step‐by‐step
                txtObfuscated.Text = originalCode;
                HighlightSyntax(txtObfuscated);
                Log("Slow Mode: Ready to step through each transformation.", "info");
            }
        }

        private void btnNextStep_Click(object sender, EventArgs e)
        {
            // If we just finished the last step, nothing else to do.
            if (_currentStepIndex >= _steps.Count)
            {
                // ―――――――――――――――――――――――――――――――――――――
                // NEW SECTION: once slow‐mode is done, append M_mgbkf(...) method
                {
                    var rootFinal = (CompilationUnitSyntax)_currentTree.GetRoot();
                    var classNode = rootFinal
                                    .DescendantNodes()
                                    .OfType<ClassDeclarationSyntax>()
                                    .FirstOrDefault();
                    if (classNode != null &&
                        !classNode.Members.OfType<MethodDeclarationSyntax>()
                                   .Any(m => m.Identifier.Text == "M_mgbkf"))   // ←── CHANGED: use literal
                    {
                        // Create a fresh Obfuscator instance just to get CreateDecryptMethod()
                        var tempObfuscator = new Obfuscator(_currentTree, Log, obfuscatorSettings);  // ←── CHANGED: avoid reusing variable name
                        var decryptMethod = tempObfuscator.CreateDecryptMethod();                 // ←── CHANGED: now it’s public

                        var newClassNode = classNode.AddMembers(decryptMethod);
                        rootFinal = rootFinal.ReplaceNode(classNode, newClassNode);
                        _currentTree = rootFinal.SyntaxTree;
                        txtObfuscated.Text = rootFinal.NormalizeWhitespace().ToFullString();
                        HighlightSyntax(txtObfuscated);
                    }
                }
                // ―――――――――――――――――――――――――――――――――――――

                Log("All steps applied. Slow Mode complete.", "success");
                lblStepInfo.Text = $"Done: applied {_steps.Count} steps.";
                btnNextStep.Enabled = false;
                btnObfuscate.Enabled = true;
                return;
            }

            // 0) If this is not the very first step, clear the previous highlight now:
            if (_currentStepIndex > 0)
            {
                var prev = _steps[_currentStepIndex - 1];
                // Restore background on that old span to the RichTextBox's default backcolor:
                txtObfuscated.Select(prev.Span.Start, prev.Span.Length);
                txtObfuscated.SelectionBackColor = txtObfuscated.BackColor;
                txtObfuscated.SelectionColor = Color.White; // keep the syntax‐highlighted text white
            }

            // 1) Record & display the description for the new step
            var step = _steps[_currentStepIndex];
            lblStepInfo.Text = $"Step {_currentStepIndex + 1}/{_steps.Count}: {step.Description}";
            Log($"[SlowMode] {step.Description}", "debug");

            // 2) Apply that one transform to the in‐memory tree
            var perStepObfuscator = new Obfuscator(_currentTree, Log, obfuscatorSettings);  // ←── CHANGED: new local
            _currentTree = perStepObfuscator.ApplyStep(_currentTree, step);

            // 3) Update txtObfuscated with the NEW code, but do NOT normalize whitespace:
            //    (NormalizeWhitespace would shift token positions and break future spans.)
            var newCode = _currentTree.GetRoot().ToFullString();
            txtObfuscated.Text = newCode;

            // 4) Re‐run syntax highlighting (keyword colors, string colors, etc.)
            HighlightSyntax(txtObfuscated);

            // 5) Now that syntax colors are in place, highlight the *exact* token span
            //    by selecting it and setting its BackgroundColor. We DON’T call Select(0,0) afterward.
            txtObfuscated.Focus();
            txtObfuscated.Select(step.Span.Start, step.Span.Length);
            txtObfuscated.SelectionBackColor = Color.DarkBlue;
            txtObfuscated.SelectionColor = Color.White;
            txtObfuscated.ScrollToCaret();

            // 6) Advance to the next step index
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

            // Important: we do NOT clear the selection here. That selection stays “active” (blue)
            // until the next time btnNextStep_Click is called, when we clear it at the top.
        }

        private void updateTextbox()
        {
            txtOriginal.Text = File.ReadAllText(txtPathToObfuscate.Text);
        }

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

        // Reads the selected file into txtOriginal and then highlights it
        private void UpdateOriginalText()
        {
            if (!File.Exists(txtPathToObfuscate.Text))
                return;

            string code = File.ReadAllText(txtPathToObfuscate.Text);
            txtOriginal.Text = code;
            HighlightSyntax(txtOriginal);
        }

        // Dummy placeholder for your actual obfuscation routine.
        private string RunObfuscationLogic(string input)
        {
            // … your obfuscator code …
            // For demonstration, just reverse the text:
            char[] arr = input.ToCharArray();
            Array.Reverse(arr);
            return new string(arr);
        }

        // -------------- SYNTAX HIGHLIGHTING --------------
        private void HighlightSyntax(RichTextBox rtb)
        {
            // 1) Remember the user’s selection/caret so we can restore it at the end
            int originalSelStart = rtb.SelectionStart;
            int originalSelLength = rtb.SelectionLength;

            // 2) Suspend painting/layout while we recolor
            rtb.SuspendLayout();

            // 3) Define what “default” means: plain Consolas/white
            Color defaultColor = Color.White;
            Font defaultFont = new Font("Consolas", 9F, FontStyle.Regular);

            // 4) Reset all text to default
            rtb.SelectAll();
            rtb.SelectionColor = defaultColor;
            rtb.SelectionFont = defaultFont;

            // 5) Grab the plain text once
            string allText = rtb.Text;

            // 6) Regex definitions
            // 6a) Multiline comments: /* … */
            var commentBlockPattern = new Regex(@"/\*.*?\*/", RegexOptions.Singleline);
            // 6b) Single-line comments: // … (till end of line)
            var commentLinePattern = new Regex(@"//.*?$", RegexOptions.Multiline);
            // 6c) String literals: @"…" or "…"
            var stringPattern = new Regex(@""".*?(?<!\\)""|@""[^""]*""", RegexOptions.Singleline);

            // 6d) C# keywords (representative set; expand as needed)
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
            string keywordPattern = @"\b(" + string.Join("|", keywords) + @")\b";
            var regexKeywords = new Regex(keywordPattern);

            // 7) Color multiline comment blocks (green + italic)
            foreach (Match m in commentBlockPattern.Matches(allText))
            {
                rtb.Select(m.Index, m.Length);
                rtb.SelectionColor = Color.Green;
                rtb.SelectionFont = new Font("Consolas", 9F, FontStyle.Italic);
            }

            // 8) Color single-line comments (green + italic)
            foreach (Match m in commentLinePattern.Matches(allText))
            {
                rtb.Select(m.Index, m.Length);
                rtb.SelectionColor = Color.Green;
                rtb.SelectionFont = new Font("Consolas", 9F, FontStyle.Italic);
            }

            // 9) Color string literals (brown, regular)
            foreach (Match m in stringPattern.Matches(allText))
            {
                rtb.Select(m.Index, m.Length);
                rtb.SelectionColor = Color.Brown;
                rtb.SelectionFont = new Font("Consolas", 9F, FontStyle.Regular);
            }

            // 10) Color keywords (blue + bold), but only if still “default” (i.e. not already green/brown)
            foreach (Match m in regexKeywords.Matches(allText))
            {
                rtb.Select(m.Index, m.Length);

                // If this range is still at default color, we apply keyword color.
                if (rtb.SelectionColor == defaultColor)
                {
                    rtb.SelectionColor = Color.Blue;
                    rtb.SelectionFont = new Font("Consolas", 9F, FontStyle.Bold);
                }
            }

            // 11) Restore the user’s original selection/caret
            rtb.Select(originalSelStart, originalSelLength);
            rtb.SelectionColor = defaultColor;

            // 12) Resume painting/layout
            rtb.ResumeLayout();
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
        public List<ObfuscationStep> Steps { get; } = new List<ObfuscationStep>();
        private SemanticModel _semanticModel; // needed if you want to query variable types/values.

        // Global mapping för klasser, metoder etc.
        private Dictionary<string, string> nameMap = new Dictionary<string, string>();
        // En stack för lokala (scope-specifika) namn
        private Stack<Dictionary<string, string>> localScopeStack = new Stack<Dictionary<string, string>>();
        private Random random = new Random();
        private Action<string, string> log;
        private bool decryptStringInjected = false;
        public readonly string DecryptMethodName = "M_mgbkf";       // ←─ Exposed for reference in Form1
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

        public Obfuscator(SyntaxTree originalTree, Action<string, string> logAction, ObfuscatorSettings settings = null)
        {
            this.log = logAction;
            this.settings = settings ?? new ObfuscatorSettings();
            // Build semanticModel so we can snapshot “variables before” if needed:
            var compilation = CSharpCompilation.Create(
                "Temp",
                syntaxTrees: new[] { originalTree },
                references: new[] {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(TcpClient).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Net.IPAddress).Assembly.Location),
                MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location),"System.Runtime.dll")),
                MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location),"netstandard.dll"))
                },
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );
            _semanticModel = compilation.GetSemanticModel(originalTree);
        }

        #region Hjälpfunktioner för namn

        private bool IsMainMethod(MethodDeclarationSyntax method)
        {
            // Case 1: a classic `static void Main(...)`
            bool isVoidMain = method.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword))
                             && method.ReturnType is PredefinedTypeSyntax pts
                             && pts.Keyword.IsKind(SyntaxKind.VoidKeyword);

            // Case 2: C# 7.1+ entry‐point signature: `static async Task Main(string[] args)`
            bool isAsyncTaskMain =
                method.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword))
             && method.Modifiers.Any(m => m.IsKind(SyntaxKind.AsyncKeyword))
             && method.ReturnType is IdentifierNameSyntax idName
             && idName.Identifier.Text == "Task";

            return isVoidMain || isAsyncTaskMain;
        }

        private void ProcessMethod(MethodDeclarationSyntax method)
        {
            if (method.ExplicitInterfaceSpecifier != null
                || method.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)))
            {
                log($"[SkipMethod] Preserving {method.Identifier.Text}", "info");
                return;
            }

            if (method.Identifier.Text == "Main" && IsMainMethod(method))
                return;

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

        // Lokal (scope-specifik) mapping
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

            // 1) Generate a new local‐map name if needed
            string newName = LookupLocalMapping(originalName);
            if (newName == null)
            {
                newName = "P_" + GenerateRandomName();
                AddLocalMapping(originalName, newName);

                // 2) Record the “param rename” step
                Steps.Add(new ObfuscationStep
                {
                    Description = $"Renaming parameter '{originalName}' → '{newName}'",
                    Span = node.Identifier.Span,
                    BeforeText = originalName,
                    AfterText = newName,
                    VariableSnapshot = null
                });
            }

            // 3) We do not rewrite the token HERE—ApplyStep will do that later.
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

                    // Record the field rename
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

                    // Record the local‐variable rename
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

            // Visit nested initializers/expressions so they get renamed first:
            var visited = (VariableDeclaratorSyntax)base.VisitVariableDeclarator(node);

            // ──────────────────────────────────────────────────────────────────────
            // If this is a local (non‐field) and there is no initializer, give it
            // a default initializer based on its declared type:
            //    int    → = 0;
            //    double → = 0.0;
            //    char   → = '\0';
            //    bool   → = false;
            //    string → (no initializer needed; default is already null)
            //    any other type → = default(ThatType);
            // ──────────────────────────────────────────────────────────────────────
            if (!isField && visited.Initializer == null)
            {
                if (node.Parent is VariableDeclarationSyntax varDecl &&
                    varDecl.Type is TypeSyntax declaredType)
                {
                    ExpressionSyntax defaultExpr;

                    // Decide which default expression to use:
                    if (declaredType is PredefinedTypeSyntax pts)
                    {
                        // Predefined (int, double, char, bool, etc.)
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
                                // e.g. "int" → 0
                                defaultExpr = SyntaxFactory.LiteralExpression(
                                    SyntaxKind.NumericLiteralExpression,
                                    SyntaxFactory.Literal(0)
                                );
                                break;

                            case SyntaxKind.DoubleKeyword:
                            case SyntaxKind.FloatKeyword:
                            case SyntaxKind.DecimalKeyword:
                                // e.g. "double" → 0.0
                                defaultExpr = SyntaxFactory.LiteralExpression(
                                    SyntaxKind.NumericLiteralExpression,
                                    SyntaxFactory.Literal(0.0)
                                );
                                break;

                            case SyntaxKind.CharKeyword:
                                // e.g. "char" → '\0'
                                defaultExpr = SyntaxFactory.LiteralExpression(
                                    SyntaxKind.CharacterLiteralExpression,
                                    SyntaxFactory.Literal('\0')
                                );
                                break;

                            case SyntaxKind.BoolKeyword:
                                // e.g. "bool" → false
                                defaultExpr = SyntaxFactory.LiteralExpression(
                                    SyntaxKind.FalseLiteralExpression
                                );
                                break;

                            case SyntaxKind.StringKeyword:
                                // string default is already null; we can omit initializer
                                defaultExpr = null;
                                break;

                            default:
                                // Any other primitive: fallback to default(ThatType)
                                defaultExpr = SyntaxFactory.DefaultExpression(declaredType);
                                break;
                        }
                    }
                    else
                    {
                        // Non‐predefined (class, struct, etc.) → default(DeclaredType)
                        defaultExpr = SyntaxFactory.DefaultExpression(declaredType);
                    }

                    if (defaultExpr != null)
                    {
                        // Attach “= <defaultExpr>” as the initializer
                        var defaultInit = SyntaxFactory.EqualsValueClause(defaultExpr);
                        visited = visited.WithInitializer(defaultInit);
                    }
                }
            }

            // Finally, rename the identifier to its obfuscated name:
            visited = visited.WithIdentifier(SyntaxFactory.Identifier(newName));
            return visited;
        }

        // IdentifierName: kolla först i lokala scopes, annars i global mapping
        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
        {
            string original = node.Identifier.Text;
            string newName = LookupLocalMapping(original) ?? LookupGlobalMapping(original);
            if (newName != null && original != newName)
            {
                // Record each reference as well (optional)
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


        // Vid metodanrop kontrolleras även lokala mappings
        public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (node.Expression is IdentifierNameSyntax identifier)
            {
                string originalName = identifier.Identifier.Text;
                string newName = LookupLocalMapping(originalName) ?? LookupGlobalMapping(originalName);
                if (newName != null && originalName != newName)
                {
                    // Record a step: “Replacing invocation foo() → M_abCdEf()”
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

        public SyntaxTree ApplyStep(SyntaxTree currentTree, ObfuscationStep step)
        {
            // 1) Get the existing root
            var root = (CompilationUnitSyntax)currentTree.GetRoot();

            // 2) Attempt to find the token at the recorded span
            var token = root.FindToken(step.Span.Start);

            // Determine if the “AfterText” is a multi-part expression (call + call + …)
            bool isConcatExpr = step.AfterText.Contains("+") || step.AfterText.Contains("(");

            // If it’s meant to be a concatenation of M_mgbkf(...) calls, we must replace
            // the entire LiteralExpression node, not just one token.  Otherwise we do
            // a token replacement as before.

            if (isConcatExpr)
            {
                //
                // ─── Replace the whole LiteralExpressionSyntax with a parsed Expression ───
                //
                // First: find the LiteralExpressionSyntax whose token span matches step.Span.
                //
                var literalNode = root.DescendantNodes()
                                      .OfType<LiteralExpressionSyntax>()
                                      .FirstOrDefault(l => l.Token.Span == step.Span);

                // If we didn’t find it by an exact span‐match, fall back to searching by text:
                if (literalNode == null)
                {
                    literalNode = root.DescendantNodes()
                                      .OfType<LiteralExpressionSyntax>()
                                      .FirstOrDefault(l => l.GetText().ToString() == step.BeforeText);
                }

                if (literalNode == null)
                {
                    // Couldn’t locate the string-literal node, so we skip this step
                    log($"[Warning] Could not locate literal node '{step.BeforeText}' for replacement; skipping.", "warn");
                    return currentTree;
                }

                // Now parse the AfterText (e.g. "M_mgbkf(\"MQ==\") + M_mgbkf(\"Mg==\") + …")
                // into a real ExpressionSyntax.  We assume AfterText is valid C# for a chain of calls.
                ExpressionSyntax newExpr;
                try
                {
                    newExpr = SyntaxFactory.ParseExpression(step.AfterText);
                }
                catch
                {
                    log($"[Warning] Failed to parse concatenation expression: {step.AfterText}; skipping.", "warn");
                    return currentTree;
                }

                // Preserve the same leading/trailing trivia that the old literal had
                newExpr = newExpr
                            .WithLeadingTrivia(literalNode.GetLeadingTrivia())
                            .WithTrailingTrivia(literalNode.GetTrailingTrivia());

                // Replace that entire LiteralExpression node
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
                // ─── Old “token-only” replacement for identifiers or simple literals ───

                bool matchesBySpan = false;
                if (token.ValueText == step.BeforeText.Trim('"') ||
                    token.ToString() == step.BeforeText)
                {
                    matchesBySpan = true;
                }

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
                    // Fallback: scan all descendant tokens
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
                    log($"[Warning] Could not locate token '{step.BeforeText}' for replacement; skipping.", "warn");
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
                    // AfterText is a single literal (e.g. "\"hello\""), so strip quotes and re-create.
                    newToken = SyntaxFactory.Literal(step.AfterText.Trim('"'))
                                           .WithLeadingTrivia(tokenToReplace.LeadingTrivia)
                                           .WithTrailingTrivia(tokenToReplace.TrailingTrivia);
                }
                else
                {
                    log($"[Warning] Token kind {tokenToReplace.Kind()} not supported for replacement; skipping.", "warn");
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

        #region Övriga överskrivningar (global mapping)

        public override SyntaxNode VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            // If there's a 'const' modifier on a local declaration, remove it entirely.
            if (node.Modifiers.Any(tok => tok.IsKind(SyntaxKind.ConstKeyword)))
            {
                // Build a new modifier list without 'const'
                var newModifiers = new SyntaxTokenList(
                    node.Modifiers
                        .Where(tok => !tok.IsKind(SyntaxKind.ConstKeyword))
                        .ToArray()
                );

                // Create a new LocalDeclarationStatementSyntax without 'const'
                // (The initializer—potentially a M_mgbkf(...) chain—stays the same.)
                var newNode = node.WithModifiers(newModifiers);
                return base.VisitLocalDeclarationStatement(newNode);
            }

            return base.VisitLocalDeclarationStatement(node);
        }


        public override SyntaxNode VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            if (node.IsKind(SyntaxKind.StringLiteralExpression))
            {
                if (node.Parent is CaseSwitchLabelSyntax) return node;

                string originalString = node.Token.ValueText;
                ExpressionSyntax replacementExpr;

                if (!settings.EnableStringSplitting || originalString.Length <= 4)
                {
                    string encrypted = Convert.ToBase64String(
                        System.Text.Encoding.UTF8.GetBytes(originalString)
                    );
                    log($"[VisitLiteralExpression] Encrypting '{originalString}' → {DecryptMethodName}(\"{encrypted}\")", "info");

                    // Record this single‐call encryption as a step
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
                    log($"[VisitLiteralExpression] Splitting and encrypting '{originalString}'", "info");
                    int pieces = Math.Min(5, originalString.Length);
                    pieces = random.Next(2, pieces + 1);
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

                    // Build expressions M_mgbkf("…") + M_mgbkf("…") + …
                    var partExprs = new List<ExpressionSyntax>();
                    foreach (var piece in splitPieces)
                    {
                        string encPart = Convert.ToBase64String(
                            System.Text.Encoding.UTF8.GetBytes(piece)
                        );
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

                    // Create a textual “AfterText” for logging (e.g. “M_mgbkf("…") + M_mgbkf("…")”)
                    string after = string.Join(" + ",
                        splitPieces
                          .Select(p => $"{DecryptMethodName}(\"{Convert.ToBase64String(Encoding.UTF8.GetBytes(p))}\")")
                    );

                    // Record the step once, using the full concatenation as AfterText
                    Steps.Add(new ObfuscationStep
                    {
                        Description = $"Splitting/encrypting '{originalString}' into {pieces} parts",
                        Span = node.Token.Span,
                        BeforeText = $"\"{originalString}\"",
                        AfterText = after,
                        VariableSnapshot = null
                    });

                    // Build the actual syntax tree concatenation
                    ExpressionSyntax concatenated = partExprs[0];
                    for (int i = 1; i < partExprs.Count; i++)
                        concatenated = SyntaxFactory.BinaryExpression(
                            SyntaxKind.AddExpression,
                            concatenated,
                            partExprs[i]
                        );
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

            // Decide the new name now (but we won’t rewrite immediately).
            string newName = LookupGlobalMapping(originalClassName);
            if (newName == null)
            {
                newName = "C_" + GenerateRandomName();
                AddGlobalMapping(originalClassName, "C");
                newName = LookupGlobalMapping(originalClassName);
            }

            // Record the “before” text and “after” text:
            var beforeText = node.Identifier.Text;
            var afterText = newName;
            var span = node.Identifier.Span;

            Steps.Add(new ObfuscationStep
            {
                Description = $"Renaming class '{originalClassName}' → '{newName}'",
                Span = span,
                BeforeText = beforeText,
                AfterText = afterText,
                VariableSnapshot = null
            });

            // Don’t actually return the rewritten node yet—just record step. Instead, let the base visitor go on,
            // but “simulate” that we will rename at StepIndex X in Phase B.
            // For now we return the unmodified node; it will be replaced later step‐by‐step.
            return base.VisitClassDeclaration(node);
        }

        // Vi fortsätter att använda din kontrollflödesomvandling i metoder
        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var originalName = node.Identifier.Text;

            // Skip public/explicit interface methods exactly as before:
            if (node.ExplicitInterfaceSpecifier != null
                || node.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)))
            {
                log($"[SkipMethod] Preserving {originalName}", "info");
                return base.VisitMethodDeclaration(node);
            }
            // Skip Main if entry‐point:
            if (originalName == "Main" && IsMainMethod(node))
                return base.VisitMethodDeclaration(node);

            // 1) Generate a new obfuscated name right now
            string newName = LookupGlobalMapping(originalName);
            if (newName == null)
            {
                newName = "M_" + GenerateRandomName();
                AddGlobalMapping(originalName, "M");
                newName = LookupGlobalMapping(originalName);
            }

            // 2) Record the step (old→new) before we actually rewrite anything
            Steps.Add(new ObfuscationStep
            {
                Description = $"Renaming method '{originalName}' → '{newName}'",
                Span = node.Identifier.Span,
                BeforeText = originalName,
                AfterText = newName,
                VariableSnapshot = null
            });

            // 3) Now that the mapping + step are recorded, recurse into parameters/body
            PushLocalScope();
            // Visit parameters so that we also rename each parameter (see VisitParameter below)
            var visitedParams = (ParameterListSyntax)Visit(node.ParameterList);
            node = node.WithParameterList(visitedParams);

            if (node.Body != null)
            {
                var newBody = (BlockSyntax)Visit(node.Body);
                node = node.WithBody(newBody);
            }
            PopLocalScope();

            // 4) Return the unmodified node for now—actual token replacement happens later in ApplyStep.
            return node;
        }


        public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            // If this field has a 'const' modifier, we need to convert it to 'static readonly'
            if (node.Modifiers.Any(tok => tok.IsKind(SyntaxKind.ConstKeyword)))
            {
                // 1) Drop 'const' from the modifiers
                var newModifiers = new SyntaxTokenList(
                    node.Modifiers
                        .Where(tok => !tok.IsKind(SyntaxKind.ConstKeyword))
                        .ToArray()
                );

                // 2) Determine where to insert 'static' and 'readonly'
                int insertPos = 0;
                // If there's an accessibility keyword (public/private/internal/protected), insert after that
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

                // Insert 'static' then 'readonly'
                newModifiers = newModifiers.Insert(insertPos, SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword));
                newModifiers = newModifiers.Insert(insertPos, SyntaxFactory.Token(SyntaxKind.StaticKeyword));

                // 3) Rebuild the FieldDeclaration with the new modifier list
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

        #region Övrigt

        public override SyntaxNode VisitSwitchStatement(SwitchStatementSyntax node)
        {
            log($"[VisitSwitchStatement] Obfuscating switch statement.", "info");

            // Record a step that describes “flattening this switch”:
            var entireSwitchSpan = node.SwitchKeyword.Span; // or entire node.Span if you prefer
            Steps.Add(new ObfuscationStep
            {
                Description = $"Flattening switch at line {node.GetLocation().GetLineSpan().StartLinePosition.Line + 1}",
                Span = entireSwitchSpan,
                BeforeText = node.ToString().Split(Environment.NewLine)[0] + "...", // first line of switch
                AfterText = "/* flattened state‐machine */",
                VariableSnapshot = null
            });

            // 1) Generate a fresh local name for “state”.
            string stateVarName = "V_" + GenerateRandomName();
            AddLocalMapping(stateVarName, stateVarName);

            // Visit the switch‐expression so any nested identifiers get renamed.
            ExpressionSyntax visitedExpr = (ExpressionSyntax)Visit(node.Expression);

            // Detect if *any* case‐label is a string literal:
            bool isStringSwitch = node.Sections
                .SelectMany(sec => sec.Labels)
                .OfType<CaseSwitchLabelSyntax>()
                .Any(lbl => lbl.Value is LiteralExpressionSyntax lit
                            && lit.IsKind(SyntaxKind.StringLiteralExpression));

            // Detect if *any* case‐label is a char literal (e.g., case '+', '-', '*', '/'):
            bool isCharSwitch = node.Sections
                .SelectMany(sec => sec.Labels)
                .OfType<CaseSwitchLabelSyntax>()
                .Any(lbl => lbl.Value is LiteralExpressionSyntax lit2
                            && lit2.IsKind(SyntaxKind.CharacterLiteralExpression));

            // If this switch is string‐based **or** char‐based, leave it alone:
            if (isStringSwitch || isCharSwitch)
            {
                // Rename any identifiers inside, but keep the original switch structure.
                return base.VisitSwitchStatement(node);
            }

            //
            // ───────────── NON‐STRING, NON‐CHAR SWITCH (e.g. int‐only) ─────────────
            //
            // You can now safely produce your “flatten into int state” logic here. For example:
            //  (a) int state = -1;
            //  (b) nested if/else comparing visitedExpr to each integer‐case
            //  (c) while (state != -1) { switch(state) { … } }
            //

            // (a) Emit “int state = -1;”
            var declStmt = SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.PredefinedType(
                        SyntaxFactory.Token(SyntaxKind.IntKeyword)))
                .WithVariables(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(
                            SyntaxFactory.Identifier(stateVarName))
                        .WithInitializer(
                            SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.PrefixUnaryExpression(
                                    SyntaxKind.UnaryMinusExpression,
                                    SyntaxFactory.LiteralExpression(
                                        SyntaxKind.NumericLiteralExpression,
                                        SyntaxFactory.Literal(-1)
                                    )
                                )
                            )
                        )
                    )
                )
            );

            // (b) Collect all non‐default sections and map each index → random key
            var rawSections = node.Sections
                .Select((sec, idx) => new { Section = sec, Index = idx })
                .Where(x => !x.Section.Labels
                    .OfType<DefaultSwitchLabelSyntax>().Any())
                .OrderBy(x => random.Next())  // you can still shuffle if you like
                .ToList();

            var caseMappings = new Dictionary<int, int>();
            foreach (var entry in rawSections)
            {
                caseMappings[entry.Index] = random.Next(1000, 9999);
            }

            // Build a nested if/else chain comparing visitedExpr to each integer‐literal label
            IfStatementSyntax ifChain = null;
            IfStatementSyntax currentIf = null;
            bool firstIf = true;

            foreach (var entry in rawSections)
            {
                // Find the int‐literal in this case (e.g. `case 42:`)
                var intLabel = entry.Section.Labels
                    .OfType<CaseSwitchLabelSyntax>()
                    .Select(lbl => lbl.Value)
                    .OfType<LiteralExpressionSyntax>()
                    .FirstOrDefault(lit => lit.IsKind(SyntaxKind.NumericLiteralExpression));

                if (intLabel == null)
                    continue;

                int literalInt = (int)intLabel.Token.Value;
                int thisKey = caseMappings[entry.Index];

                // Build “(visitedExpr == <literalInt>)”
                var cmp = SyntaxFactory.BinaryExpression(
                    SyntaxKind.EqualsExpression,
                    visitedExpr,
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal(literalInt)
                    )
                );

                // Build “state = <thisKey>;”
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

            // (c) Build the flattened “while(state != -1) { switch(state) { … } }”:
            var flattenedSections = new List<SwitchSectionSyntax>();
            int count = rawSections.Count;

            for (int i = 0; i < count; i++)
            {
                var entry = rawSections[i];
                int thisKey = caseMappings[entry.Index];
                bool isLast = (i == count - 1);
                int nextKey = isLast
                    ? -1
                    : caseMappings[rawSections[i + 1].Index];

                // Visit the original statements so nested obfuscations still apply:
                var visitedStmts = entry.Section.Statements
                    .Select(s => (StatementSyntax)Visit(s))
                    .ToList();

                // After them, assign “state = nextKey;”
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
                // Then “break;”
                visitedStmts.Add(SyntaxFactory.BreakStatement());

                // Build “case <thisKey>: …”
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

            // Append “default: break;” at the end
            var defaultSectionEnd = SyntaxFactory.SwitchSection(
                SyntaxFactory.SingletonList<SwitchLabelSyntax>(
                    SyntaxFactory.DefaultSwitchLabel()),
                SyntaxFactory.SingletonList<StatementSyntax>(
                    SyntaxFactory.BreakStatement())
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

            // 5) Return a Block containing:
            //    (A) declStmt, (B) assignChainOrInit (if not null), (C) whileLoop
            if (assignChainOrInit != null)
            {
                return SyntaxFactory.Block(declStmt, assignChainOrInit, whileLoop);
            }
            else
            {
                return SyntaxFactory.Block(declStmt, whileLoop);
            }
        }


        private bool IsSystemType(string identifier) =>
            systemIdentifiers.Contains(identifier) ||
            identifier.StartsWith("System.") ||
            identifier.StartsWith("Microsoft.");

        public MethodDeclarationSyntax CreateDecryptMethod() =>
            SyntaxFactory.ParseMemberDeclaration($@"
                private static string {DecryptMethodName}(string {decryptStringVariableName})
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

    public class ObfuscationStep
    {
        // Short human‐readable description (e.g. “Renaming method ‘Foo’ → ‘M_abCdEf’”)
        public string Description { get; set; }

        // The TextSpan in the CURRENT code where this step will apply.
        public TextSpan Span { get; set; }

        // The “before” snippet (for display). If null, you can grab it from the code viewer on‐demand.
        public string BeforeText { get; set; }

        // The “after” snippet (what it becomes).
        public string AfterText { get; set; }

        // (Optional) A snapshot of any variable’s old value, if you want to show “X was null before, now is 'M_xxx'”.
        public string VariableSnapshot { get; set; }
    }
}
