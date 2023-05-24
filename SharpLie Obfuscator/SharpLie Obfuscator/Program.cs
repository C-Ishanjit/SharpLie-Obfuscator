using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SharpLieObfuscator
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Please provide the input file name as a command-line argument.");
                return;
            }

            string inputFileName = args[0];

            if (!File.Exists(inputFileName))
            {
                Console.WriteLine("Input file does not exist.");
                return;
            }

            string code = File.ReadAllText(inputFileName);

            string obfuscatedCode = ObfuscateCode(code);

            string outputFileName = Path.GetFileNameWithoutExtension(inputFileName) + "-secured.cs";
            string outputPath = Path.Combine(Path.GetDirectoryName(inputFileName), outputFileName);
            File.WriteAllText(outputPath, obfuscatedCode);

            Console.WriteLine("Code obfuscated successfully. Output file: " + outputPath);
        }

        static string ObfuscateCode(string code)
        {
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);
            CompilationUnitSyntax root = syntaxTree.GetRoot() as CompilationUnitSyntax;

            SyntaxNode newRoot = RenameVariables(root, syntaxTree);
            newRoot = AddSpamComments(newRoot);

            return newRoot.ToFullString();
        }

        static SyntaxNode RenameVariables(SyntaxNode node, SyntaxTree syntaxTree)
        {
            SemanticModel semanticModel = GetSemanticModel(syntaxTree);

            VariableRenamingRewriter renamingRewriter = new VariableRenamingRewriter(semanticModel);
            SyntaxNode newRoot = renamingRewriter.Visit(node);

            return newRoot;
        }

        static SemanticModel GetSemanticModel(SyntaxTree syntaxTree)
        {
            var mscorlib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
            var compilation = CSharpCompilation.Create("TempCompilation", syntaxTrees: new[] { syntaxTree }, references: new[] { mscorlib });
            var model = compilation.GetSemanticModel(syntaxTree);

            return model;
        }

        static SyntaxNode AddSpamComments(SyntaxNode node)
        {
            CommentAddingRewriter commentAddingRewriter = new CommentAddingRewriter();
            SyntaxNode newRoot = commentAddingRewriter.Visit(node);

            return newRoot;
        }

        class VariableRenamingRewriter : CSharpSyntaxRewriter
        {
            private readonly SemanticModel semanticModel;
            private readonly Dictionary<string, string> variableMap;

            public VariableRenamingRewriter(SemanticModel semanticModel)
            {
                this.semanticModel = semanticModel;
                this.variableMap = new Dictionary<string, string>();
            }

            public override SyntaxNode VisitVariableDeclarator(VariableDeclaratorSyntax node)
            {
                var newVariable = (VariableDeclaratorSyntax)base.VisitVariableDeclarator(node);

                var symbol = semanticModel.GetDeclaredSymbol(node);
                if (symbol != null)
                {
                    var newName = "v_" + Guid.NewGuid().ToString("N");
                    variableMap[symbol.Name] = newName;

                    newVariable = newVariable.WithIdentifier(SyntaxFactory.Identifier(newName));
                }

                return newVariable;
            }

            public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
            {
                var symbol = semanticModel.GetSymbolInfo(node).Symbol;

                if (symbol is ILocalSymbol localSymbol && variableMap.TryGetValue(localSymbol.Name, out var newName))
                {
                    return SyntaxFactory.IdentifierName(newName).WithTriviaFrom(node);
                }

                return base.VisitIdentifierName(node);
            }
        }

        class CommentAddingRewriter : CSharpSyntaxRewriter
        {
            private static readonly Random random = new Random();

            public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
            {
                if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia))
                {
                    return trivia;
                }

                if (trivia.IsKind(SyntaxKind.EndOfLineTrivia))
                {
                    string commentBlock = string.Empty;

                    for (int i = 0; i < 100; i++)
                    {
                        string arabicComment = GenerateRandomArabicComment();
                        commentBlock += $"/* {arabicComment} */" + Environment.NewLine;
                    }

                    return SyntaxFactory.Comment(commentBlock);
                }

                return trivia;
            }

            private string GenerateRandomArabicComment()
            {
                // List of Arabic words to generate comments from
                List<string> arabicWords = new List<string>
        {
            "مرحبًا",
            "برمجة",
            "مثال",
            "مكتبة",
            "اختبار",
            "تطبيق",
            "محرر",
            "مصفوفة",
            "عنصر",
            "مستخدم",
            "SharpLie Winning"
        };

                StringBuilder commentBuilder = new StringBuilder();

                // Generate a longer comment by concatenating random Arabic words
                int commentLength = random.Next(20, 41); // Random length between 20 and 40 words
                for (int i = 0; i < commentLength; i++)
                {
                    string arabicWord = arabicWords[random.Next(arabicWords.Count)];
                    commentBuilder.Append(arabicWord).Append(" ");
                }

                return commentBuilder.ToString();
            }
        }







    }
}
