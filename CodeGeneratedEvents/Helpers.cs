using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ModernUO.CodeGeneratedEvents;

public static class Helpers
{
    public static void AppendContainingTypes(this StringBuilder builder, INamedTypeSymbol typeSymbol)
    {
        if (typeSymbol.ContainingType != null)
        {
            AppendContainingTypes(builder, typeSymbol.ContainingType);
        }

        // Append the access modifier
        builder.Append(typeSymbol.DeclaredAccessibility.ToAccessModifierString());

        // Append the type declaration modifiers (abstract, static)
        if (typeSymbol.IsAbstract && !typeSymbol.IsSealed) // abstract but not static
        {
            builder.Append("abstract ");
        }
        else if (typeSymbol.IsStatic)
        {
            builder.Append("static ");
        }

        // Append the type declaration
        builder.Append("partial ")
            .Append(typeSymbol.TypeKind.GetTypeKindKeyword())
            .Append(' ')
            .Append(typeSymbol.Name);

        // Append type parameters if any
        if (typeSymbol.TypeParameters.Length > 0)
        {
            builder
                .Append('<')
                .Append(string.Join(", ", typeSymbol.TypeParameters.Select(tp => tp.Name)))
                .Append('>');
        }

        builder.AppendLine();
        builder.AppendLine("{");
    }

    private static string ToAccessModifierString(this Accessibility accessibility)
    {
        return accessibility switch
        {
            Accessibility.Public               => "public ",
            Accessibility.Private              => "private ",
            Accessibility.Internal             => "internal ",
            Accessibility.Protected            => "protected ",
            Accessibility.ProtectedOrInternal  => "protected internal ",
            Accessibility.ProtectedAndInternal => "private protected ",
            _                                  => string.Empty, // In case of something like Accessibility.NotApplicable
        };
    }

    public static string FormatParameterInvocation(this IMethodSymbol methodSymbol)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < methodSymbol.Parameters.Length; i++)
        {
            var parameter = methodSymbol.Parameters[i];
            if (parameter.RefKind != RefKind.None)
            {
                builder.Append(parameter.RefKind.GetRefKindKeyword()).Append(' ');
            }

            builder.Append(parameter.Name);
            if (i < methodSymbol.Parameters.Length - 1)
            {
                builder.Append(", ");
            }
        }

        return builder.ToString();
    }

    public static string GetTypeKindKeyword(this TypeKind typeKind)
    {
        return typeKind switch
        {
            TypeKind.Class     => "class",
            TypeKind.Struct    => "struct",
            TypeKind.Interface => "interface",
            _                  => "class" // Default fallback
        };
    }

    public static string GetRefKindKeyword(this RefKind refKind)
    {
        return refKind switch
        {
            RefKind.None => "",
            RefKind.Ref  => "ref",
            RefKind.Out  => "out",
            RefKind.In   => "in",
            _            => "" // Default fallback
        };
    }

    public static string ToFriendlyString(this Accessibility accessibility) => SyntaxFacts.GetText(accessibility);

    public static void FormatParameter(this StringBuilder builder, IParameterSymbol parameter)
    {
        // Add parameter modifiers (ref, out, in)
        if (parameter.RefKind != RefKind.None)
        {
            builder.Append(parameter.RefKind.GetRefKindKeyword()).Append(' ');
        }

        // Add the parameter type
        builder.Append(parameter.Type.ToDisplayString()).Append(' ');

        // Add the parameter name
        builder.Append(parameter.Name);

        // Add default value if it exists
        if (parameter.HasExplicitDefaultValue)
        {
            builder.Append(" = ").Append(parameter.ExplicitDefaultValue ?? "default");
        }
    }

    public static string ResolveConstantValue(this ExpressionSyntax expression, SemanticModel semanticModel)
    {
        // var symbolInfo = semanticModel.GetSymbolInfo(expression);
        var constantValue = semanticModel.GetConstantValue(expression);

        return constantValue is { HasValue: true, Value: string stringValue } ? stringValue : null;
    }
}
