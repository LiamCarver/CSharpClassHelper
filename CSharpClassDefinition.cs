using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSharpClassHelper
{
    public class CSharpProperty : CSharpVariable
    {
        public string Access { get; set; }
        public bool IsGetOverride { get; set; }
        public string OverrideValue { get; set; }
    }

    public class CSharpAttribute
    {
        public string Name { get; set; }
        public IEnumerable<string> Arguments { get; set; }

        public override string ToString()
        {
            if (Arguments != null && Arguments.Any())
            {
                return $"[{Name}(\"{string.Join(",", Arguments)}\")]";
            }
            else
            {
                return $"[{Name}]";
            }
        }
    }

    public class CSharpParameter : CSharpVariable
    {
        public bool IsExtensionParameter { get; set; }
    }

    public class CSharpConstant : CSharpVariable
    {
        public string Access { get; set; }
        public string Value { get; set; }
    }

    public class CSharpVariable
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }

    public partial class CSharpMethod
    {
        public IEnumerable<string> MethodKeyWords { get; set; }
        public string Name { get; set; }
        public IEnumerable<CSharpParameter> Parameters { get; set; }
        public IEnumerable<string> MethodLines { get; set; } = new List<string>();
        public string ReturnType { get; set; }
        public IEnumerable<CSharpAttribute> Attributes { get; set; }
        public IEnumerable<string> BaseConstructorArguments { get; set; }
    }

    public class CSharpClassDefinition
    {
        public IEnumerable<string> UsingStatements { get; set; } = new List<string>();
        public string Namespace { get; set; }
        public IEnumerable<string> ClassKeyWords { get; set; } = new List<string>();
        public string Name { get; set; }
        public IEnumerable<CSharpProperty> Properties { get; set; }
        public IEnumerable<CSharpConstant> Constants { get; set; }
        public List<CSharpMethod> Methods { get; set; }
        public string Implementations { get; set; }
        public IEnumerable<CSharpAttribute> Attributes { get; set; } = new List<CSharpAttribute>();
        public bool IsInterface { get; set; }
        public List<CSharpClassDefinition> InnerClasses { get; set; }
        public bool IsInnerClass { get; set; }

        public string ToString(int startingTabCount = 0)
        {
            var stringBuilder = new StringBuilder();
            var tabCount = startingTabCount;

            AddUsingStatements(stringBuilder);
            AddNamespace(stringBuilder);

            Action mainCodeBlock = () =>
            {
                AddClassAttributes(stringBuilder, tabCount);
                AddClassName(stringBuilder, tabCount);

                // class code block
                stringBuilder.AddCodeBlock(ref tabCount, () =>
                {
                    AddInnerClasses(stringBuilder, tabCount);
                    AddConstants(stringBuilder, tabCount);
                    AddProperties(stringBuilder, tabCount);
                    AddMethods(stringBuilder, tabCount);
                });

            };

            if (IsInnerClass)
            {
                mainCodeBlock();
            }
            else
            {
                // namespace code block
                stringBuilder.AddCodeBlock(ref tabCount, mainCodeBlock);
            }

            return stringBuilder.ToString();
        }

        #region ToString Helper Methods

        private void AddInnerClasses(StringBuilder stringBuilder, int tabCount)
        {
            if (InnerClasses != null)
            {
                foreach (var innerClass in InnerClasses)
                {
                    stringBuilder.AppendLine(innerClass.ToString(tabCount));
                }
            }
        }

        private void AddClassAttributes(StringBuilder stringBuilder, int tabCount)
        {
            foreach (var attribute in Attributes)
            {
                stringBuilder.AppendLine(attribute.ToString(), tabCount);
            }
        }

        private void AddNamespace(StringBuilder stringBuilder)
        {
            if (IsInnerClass)
            {
                stringBuilder.AppendLine();
            }
            else
            {
                stringBuilder.AppendLine($"{CSharpKeyWords.Namespace}{CSharpSyntax.Space}{Namespace}");
            }
        }

        private void AddUsingStatements(StringBuilder stringBuilder)
        {
            if (!IsInnerClass)
            {
                stringBuilder.AppendLine(CSharpSyntax.AutoGenerationComment);
                stringBuilder.AppendLine();
            }

            if (UsingStatements.Any())
            {
                stringBuilder.AppendLine(string.Join(Environment.NewLine, UsingStatements.Select(statement => $"{CSharpKeyWords.Using}{CSharpSyntax.Space}{statement}{CSharpSyntax.StatementTerminator}")));
                stringBuilder.AppendLine();
            }
        }

        private void AddClassName(StringBuilder stringBuilder, int tabCount)
        {
            var classNameDetailList = ClassKeyWords.Concat(new string[] { IsInterface ? CSharpKeyWords.Interface : CSharpKeyWords.Class, Name });

            if (Implementations != null)
            {
                classNameDetailList = classNameDetailList.Concat(new string[] { CSharpSyntax.Colon, Implementations });
            }

            var classNameDetails = string.Join(CSharpSyntax.Space, classNameDetailList);

            stringBuilder.AppendLine(classNameDetails, tabCount);
        }

        private void AddProperties(StringBuilder stringBuilder, int tabCount)
        {
            if (Properties == null || !Properties.Any())
            {
                return;
            }

            var propertyDetails = Properties.Select(x =>
            {
                var line = $"{x.Access} {x.Type} {x.Name} ";
                if (x.IsGetOverride)
                {
                    line += $"=> {x.OverrideValue};";
                }
                else
                {
                    line += $"{CSharpSyntax.OpenCodeBlock} {CSharpKeyWords.Get}{CSharpSyntax.StatementTerminator} {CSharpKeyWords.Set}{CSharpSyntax.StatementTerminator} {CSharpSyntax.CloseCodeBlock}";
                }
                return line;
            });

            foreach (var property in propertyDetails)
            {
                stringBuilder.AppendLine(property, tabCount);
            }
        }

        private void AddConstants(StringBuilder stringBuilder, int tabCount)
        {
            if (Constants == null || !Constants.Any())
            {
                return;
            }

            foreach (var constant in Constants)
            {
                stringBuilder.AppendLine($"{constant.Access} {CSharpKeyWords.Constant} {constant.Type} {constant.Name} = {constant.Value}{CSharpSyntax.StatementTerminator}", tabCount);
            }
        }

        private void AddMethodAttributes(CSharpMethod method, StringBuilder stringBuilder, int tabCount)
        {
            if (method.Attributes == null || !method.Attributes.Any())
            {
                return;
            }

            foreach (var attribute in method.Attributes)
            {
                stringBuilder.AppendLine(attribute.ToString(), tabCount);
            }
        }

        private void AddMethodHeader(CSharpMethod method, StringBuilder stringBuilder, int tabCount)
        {
            var methodHeader = string.Join(CSharpSyntax.Space, method.MethodKeyWords.Concat(string.IsNullOrEmpty(method.Name) ? new string[] { method.ReturnType } : new string[] { method.ReturnType, method.Name }));
            var parameterDetails = string.Join($"{CSharpSyntax.Comma}{CSharpSyntax.Space}", method.Parameters.Select(x => x.IsExtensionParameter ? $"this {x.Type} {x.Name}" : $"{x.Type} {x.Name}"));
            var baseConstructorDetails = string.Empty;
            if (method.BaseConstructorArguments != null && method.BaseConstructorArguments.Any())
            {
                baseConstructorDetails = $" : base({string.Join(",", method.BaseConstructorArguments)})";
            }

            var methodHeaderEnding = IsInterface ? CSharpSyntax.StatementTerminator : string.Empty;

            stringBuilder.AppendLine($"{methodHeader}{CSharpSyntax.OpenBracket}{parameterDetails}{CSharpSyntax.CloseBracket}{baseConstructorDetails}{methodHeaderEnding}", tabCount);
        }

        private void AddMethodLines(CSharpMethod method, StringBuilder stringBuilder, int tabCount)
        {
            foreach (var line in method.MethodLines)
            {
                stringBuilder.AppendLine(line, tabCount);
            }

        }

        private void AddMethods(StringBuilder stringBuilder, int tabCount)
        {
            if (Methods == null || !Methods.Any())
            {
                return;
            }

            stringBuilder.AppendLine();

            foreach (var method in Methods)
            {
                AddMethodAttributes(method, stringBuilder, tabCount);
                AddMethodHeader(method, stringBuilder, tabCount);

                if (!IsInterface)
                {
                    stringBuilder.AddCodeBlock(ref tabCount, () =>
                    {
                        AddMethodLines(method, stringBuilder, tabCount);
                    });
                }

                stringBuilder.AppendLine();
            }
        }

        #endregion

        public static CSharpClassDefinition CreateFromTypeDeclarationSyntax(TypeDeclarationSyntax typeDeclarationSyntax, IEnumerable<string> usingStatements)
        {
            var newUsingStatements = usingStatements.ToList();
            newUsingStatements.Add(((NamespaceDeclarationSyntax)typeDeclarationSyntax.Parent).Name.ToString());

            var attributeSyntaxes = typeDeclarationSyntax.AttributeLists.Select(x => x.Attributes).SelectMany(x => x).ToList();

            var attributes = attributeSyntaxes.Select(x => new CSharpAttribute
            {
                Name = x.Name.ToString(),
                Arguments = x.ArgumentList?.Arguments.Select(y => y.ToString().Replace("\"", string.Empty)).ToArray()
            });

            return new CSharpClassDefinition
            {
                UsingStatements = newUsingStatements,
                Implementations = typeDeclarationSyntax.BaseList == null ? null : string.Join(CSharpSyntax.Comma, typeDeclarationSyntax.BaseList?.Types.Select(x => x.ToString())),
                Name = typeDeclarationSyntax.Identifier.ToString(),
                Namespace = string.Empty,
                ClassKeyWords = new List<string> { CSharpKeyWords.Public },
                Attributes = attributes,
                Methods = GetCSharpMethods(typeDeclarationSyntax),
                Properties = GetCSharpProperties(typeDeclarationSyntax),
            };
        }

        private static IEnumerable<CSharpProperty> GetCSharpProperties(TypeDeclarationSyntax typeDeclarationSyntax)
        {
            return typeDeclarationSyntax.Members
                .OfType<PropertyDeclarationSyntax>()
                .Select(x => new CSharpProperty
                {
                    Access = CSharpKeyWords.Public,
                    Name = x.Identifier.ToString(),
                    Type = x.Type.ToString(),
                });
        }

        private static List<CSharpMethod> GetCSharpMethods(TypeDeclarationSyntax typeDeclarationSyntax)
        {
            return typeDeclarationSyntax.Members
                .OfType<MethodDeclarationSyntax>()
                .Select(x => new CSharpMethod
                {
                    Name = x.Identifier.ToString(),
                    Parameters = x.ParameterList.Parameters.Select(y => new CSharpParameter { Name = y.Identifier.ToString(), Type = y.Type.ToString() }),
                    ReturnType = x.ReturnType.ToString(),
                    Attributes = x.AttributeLists.Select(y => y.Attributes).SelectMany(y => y).Select(y => new CSharpAttribute
                    {
                        Name = y.Name.ToString(),
                        Arguments = y.ArgumentList?.Arguments.Select(z => z.ToString().Replace("\"", string.Empty)).ToArray()
                    }),
                    MethodKeyWords = new List<string> { CSharpKeyWords.Public },
                    MethodLines = new List<string> { },
                }).ToList();
        }
    }

    public static class CSharpTypes
    {
        public const string String = "string";
        public const string Int = "int";
        public const string Boolean = "bool";
    }

    public static class CSharpKeyWords
    {
        public const string Public = "public";
        public const string Private = "private";
        public const string Protected = "protected";
        public const string Override = "override";
        public const string Internal = "internal";
        public const string Namespace = "namespace";
        public const string Using = "using";
        public const string Sealed = "sealed";
        public const string Static = "static";
        public const string Partial = "partial";
        public const string Class = "class";
        public const string Interface = "interface";
        public const string Get = "get";
        public const string Set = "set";
        public const string Void = "void";
        public const string Async = "async";
        public const string Constant = "const";
    }

    public static class CSharpSyntax
    {
        public const string Tab = "\t";
        public const string Space = " ";
        public const string Comma = ",";
        public const string AutoGenerationComment = "// Auto-generated code";
        public const string StatementTerminator = ";";
        public const string OpenCodeBlock = "{";
        public const string CloseCodeBlock = "}";
        public const string OpenBracket = "(";
        public const string CloseBracket = ")";
        public const string OpenSquareBracket = "[";
        public const string CloseSquareBracket = "]";
        public const string OpenAngleBracket = "<";
        public const string CloseAngleBracket = ">";
        public const string Colon = ":";
    }

    public static class CSharpHelper
    {
        public static string AddQuotes(string s)
        {
            return $"\"{s}\"";
        }
    }

    public static class StringBuilderExtensions
    {
        public static StringBuilder AppendLine(this StringBuilder stringBuilder, string line, int tabCount)
        {
            for (var i = 1; i <= tabCount; i++)
            {
                stringBuilder.Append(CSharpSyntax.Tab);
            }
            return stringBuilder.AppendLine(line);
        }

        public static void AddCodeBlock(this StringBuilder stringBuilder, ref int tabCount, Action logic)
        {
            stringBuilder.AppendLine(CSharpSyntax.OpenCodeBlock, tabCount);
            tabCount++;

            logic();

            tabCount--;
            stringBuilder.AppendLine(CSharpSyntax.CloseCodeBlock, tabCount);
        }
    }

}
