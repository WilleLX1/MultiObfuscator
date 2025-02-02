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
        public Form1()
        {
            InitializeComponent();
        }

        public void Log(string message)
        {
            // Get current time and append message to the log
            string time = DateTime.Now.ToString("HH:mm:ss");
            txtLog.Text += $"[{time}] {message}{Environment.NewLine}";
        }

        private void obfuscate()
        {
            string code = File.ReadAllText(txtPathToObfuscate.Text);

            SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetRoot();

            // Pass the Log method to the Obfuscator
            var obfuscator = new Obfuscator(Log);
            var obfuscatedRoot = obfuscator.Visit(root);

            txtObfuscated.Text = obfuscatedRoot.ToFullString();
        }

        private void btnObfuscate_Click(object sender, EventArgs e)
        {
            updateTextbox();
            obfuscate();
        }

        private void updateTextbox()
        {
            txtOrginal.Text = File.ReadAllText(txtPathToObfuscate.Text);
        }
    }

    class Obfuscator : CSharpSyntaxRewriter
    {
        private Dictionary<string, string> nameMap = new Dictionary<string, string>();
        private Random random = new Random();
        private Action<string> log; // Logging delegate
        private bool decryptStringInjected = false;
        private static readonly HashSet<string> systemIdentifiers = new HashSet<string>
        {
            "System", "Console", "Microsoft", "Convert", "Encoding", "Environment",
            "DateTime", "Exception", "Task", "List", "Enumerable", "ToUpperInvariant",
            "Trim", "ToUpper", "Substring", "ToLower", "Where", "ToList", "Join",
            "Parse", "Length"
        };

        public Obfuscator(Action<string> logAction) => this.log = logAction;

        private string GenerateRandomName()
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var generated = new string(Enumerable.Repeat(chars, 6)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            log($"[GenerateRandomName] -> {generated}"); // Use the delegate
            return generated;
        }

        private void AddNameMap(string originalName, string prefix)
        {
            if (!nameMap.ContainsKey(originalName))
            {
                string newName = prefix + "_" + GenerateRandomName();
                nameMap[originalName] = newName;
                log($"[AddNameMap] '{originalName}' -> '{newName}'"); // Use the delegate
            }
            else
            {
                log($"[AddNameMap] already exists: '{originalName}' -> '{nameMap[originalName]}'");
            }
        }

        private string LookupName(string originalName)
        {
            if (nameMap.TryGetValue(originalName, out var newName))
            {
                log($"[LookupName] Found mapping for '{originalName}' -> '{newName}'");
                return newName;
            }
            else
            {
                log($"[LookupName] WARNING: No mapping found for '{originalName}'");
                return null;
            }
        }


        // Update all other Console.WriteLine calls to use the log delegate:
        public override SyntaxNode VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            if (node.IsKind(SyntaxKind.StringLiteralExpression))
            {
                string originalString = node.Token.ValueText;
                string encryptedString = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(originalString));

                log($"[VisitLiteralExpression] '{originalString}' -> 'DecryptString(\"{encryptedString}\")'");

                return SyntaxFactory.InvocationExpression(
                    SyntaxFactory.IdentifierName("DecryptString"),
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
                visited = visited.WithIdentifier(SyntaxFactory.Identifier(newVarName));
            }

            return visited;
        }

        public override SyntaxNode VisitParameter(ParameterSyntax node)
        {
            var originalName = node.Identifier.Text;
            log($"[VisitParameter] param '{originalName}' ...");

            // Rename the parameter using the nameMap
            var newName = LookupName(originalName);
            if (newName != null)
            {
                node = node.WithIdentifier(SyntaxFactory.Identifier(newName));
            }

            return base.VisitParameter(node); // Recurse with the updated node
        }

        public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (node.Expression is IdentifierNameSyntax identifier)
            {
                string originalName = identifier.Identifier.Text;

                // Skip lookup for already-obfuscated names (M_, P_, V_, C_)
                if (originalName.StartsWith("M_") || originalName.StartsWith("P_")
                    || originalName.StartsWith("V_") || originalName.StartsWith("C_"))
                {
                    return base.VisitInvocationExpression(node);
                }

                string newName = LookupName(originalName);
                if (newName != null)
                {
                    log($"[VisitInvocationExpression] Replacing invocation '{originalName}' -> '{newName}'");
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

            var visitedClass = (ClassDeclarationSyntax)base.VisitClassDeclaration(node);
            visitedClass = visitedClass.WithIdentifier(GetRenamedIdentifier(originalClassName));

            if (!decryptStringInjected)
            {
                visitedClass = visitedClass.AddMembers(CreateDecryptMethod());
                decryptStringInjected = true;
            }

            return visitedClass;
        }

        private void ProcessMethod(MethodDeclarationSyntax method)
        {
            if (method.ExplicitInterfaceSpecifier != null ||
                method.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)))
            {
                log($"[SkipMethod] Preserving {method.Identifier.Text}");
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

            foreach (var variable in body.DescendantNodes()
                .OfType<VariableDeclaratorSyntax>())
            {
                AddNameMap(variable.Identifier.Text, "V");
            }
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (node.ExplicitInterfaceSpecifier != null ||
                node.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)))
            {
                return base.VisitMethodDeclaration(node);
            }

            var originalName = node.Identifier.Text;
            var visited = (MethodDeclarationSyntax)base.VisitMethodDeclaration(node);
            return visited.WithIdentifier(GetRenamedIdentifier(originalName));
        }

        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
        {
            if (IsSystemType(node.Identifier.Text)) return node;

            var original = node.Identifier.Text;
            var newName = LookupName(original);

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
                private static string DecryptString(string encrypted)
                {
                    if (string.IsNullOrEmpty(encrypted)) return string.Empty;
                    byte[] data = Convert.FromBase64String(encrypted);
                    return System.Text.Encoding.UTF8.GetString(data);
                }") as MethodDeclarationSyntax;

        private SyntaxToken GetRenamedIdentifier(string originalName)
        {
            var newName = LookupName(originalName);
            return SyntaxFactory.Identifier(newName ?? originalName);
        }
    }
}
