using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ModernUO.CodeGeneratedEvents;

[Generator]
public class EventGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext generatorContext)
    {
        // var compilationProvider = generatorContext.CompilationProvider;

         // Gather the OnEvent methods
        var onEventMethodsGroupedByEventName = generatorContext.SyntaxProvider.CreateSyntaxProvider(
                predicate: (syntaxNode, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    if (syntaxNode is not MethodDeclarationSyntax methodSyntax ||
                        !methodSyntax.Modifiers.Any(SyntaxKind.StaticKeyword) ||
                        methodSyntax.ReturnType.ToString() != "void")
                    {
                        return false;
                    }

                    foreach (var attributeList in methodSyntax.AttributeLists)
                    {
                        foreach (var attribute in attributeList.Attributes)
                        {
                            if (attribute.Name.ToString() == "OnEvent" &&
                                attribute.ArgumentList?.Arguments.Count == 1)
                            {
                                return true;
                            }
                        }
                    }

                    return false;
                },
                transform: (context, token) =>
                {
                    token.ThrowIfCancellationRequested();

                    var methodDeclarationSyntax = (MethodDeclarationSyntax)context.Node;
                    var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDeclarationSyntax);
                    var semanticModel = context.SemanticModel;

                    List<string> eventNames = [];
                    foreach (var attrList in methodDeclarationSyntax.AttributeLists)
                    {
                        foreach (var attr in attrList.Attributes)
                        {
                            if (attr.Name.ToString() != "OnEvent" ||
                                attr.ArgumentList?.Arguments.Count != 1)
                            {
                                continue;
                            }

                            var arg = attr.ArgumentList.Arguments[0];

                            var constantValue = arg.Expression.ResolveConstantValue(semanticModel);

                            if (constantValue != null)
                            {
                                eventNames.Add(constantValue);
                            }
                        }
                    }

                    return new { MethodSymbol = methodSymbol, EventNames = eventNames };
                })
            .Collect()
            .Select((methods, token) =>
            {
                token.ThrowIfCancellationRequested();
                Dictionary<string, List<IMethodSymbol>> eventMethodDictionary = [];

                foreach (var methodData in methods)
                {
                    foreach (var eventName in methodData.EventNames)
                    {
                        if (!eventMethodDictionary.TryGetValue(eventName, out var list))
                        {
                            eventMethodDictionary[eventName] = list = [];
                        }

                        list.Add(methodData.MethodSymbol);
                    }
                }

                return eventMethodDictionary;
            });

        // Gather the GeneratedEvent methods
        var generatedEventMethods = generatorContext.SyntaxProvider.CreateSyntaxProvider(
            predicate: (syntaxNode, token) =>
            {
                token.ThrowIfCancellationRequested();
                if (syntaxNode is not MethodDeclarationSyntax methodSyntax ||
                    !methodSyntax.Modifiers.Any(SyntaxKind.PartialKeyword) ||
                    methodSyntax.ReturnType.ToString() != "void")
                {
                    return false;
                }

                foreach (var attributeList in methodSyntax.AttributeLists)
                {
                    foreach (var attribute in attributeList.Attributes)
                    {
                        if (attribute.Name.ToString() == "GeneratedEvent" &&
                            attribute.ArgumentList?.Arguments.Count == 1)
                        {
                            return true;
                        }
                    }
                }

                return false;
            },
            transform: (context, token) =>
            {
                token.ThrowIfCancellationRequested();

                var methodDeclarationSyntax = (MethodDeclarationSyntax)context.Node;
                var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDeclarationSyntax);
                var semanticModel = context.SemanticModel;

                string eventName = null;
                foreach (var attrList in methodDeclarationSyntax.AttributeLists)
                {
                    foreach (var attr in attrList.Attributes)
                    {
                        if (attr.Name.ToString() != "GeneratedEvent" ||
                            attr.ArgumentList?.Arguments.Count != 1)
                        {
                            continue;
                        }

                        var arg = attr.ArgumentList.Arguments[0];

                        eventName = arg.Expression.ResolveConstantValue(semanticModel);

                        if (eventName != null)
                        {
                            break;
                        }
                    }
                }

                return new { MethodSymbol = methodSymbol, EventName = eventName };
            });

        var generatedCodeByClass = generatedEventMethods
            .Combine(onEventMethodsGroupedByEventName)
            .Collect()
            .SelectMany(
                (combined, token) =>
                {
                    token.ThrowIfCancellationRequested();

                    var groupedByClass = combined
                        .GroupBy(tuple => tuple.Left.MethodSymbol.ContainingType, SymbolEqualityComparer.Default)
                        .Select(
                            group =>
                            {
                                var containingType = group.Key;
                                List<GeneratedEventWithOnEvents> generatedEventsWithOnEvents = [];

                                foreach (var (generatedEventMethod, onEventDictionary) in group)
                                {
                                    generatedEventsWithOnEvents.Add(
                                        new GeneratedEventWithOnEvents(
                                            generatedEventMethod.MethodSymbol,
                                            generatedEventMethod.EventName,
                                            onEventMethods: onEventDictionary.TryGetValue(generatedEventMethod.EventName, out var oem) ? oem : []
                                        )
                                    );
                                }

                                return new GeneratedCodeForClass(
                                    containingType as INamedTypeSymbol,
                                    generatedEventsWithOnEvents
                                );
                            }
                        )
                        .ToList();

                    return groupedByClass;
                }
            );

        generatorContext.RegisterSourceOutput(generatedCodeByClass, (sourceProductionContext, info) =>
        {
            var sourceCode = GeneratedEventGenerator.Generate(
                info.ContainingType,
                info.GeneratedEventsWithOnEvents
            );

            var fqcn = info.ContainingType.ToDisplayString();

            sourceProductionContext.AddSource($"{fqcn}.GeneratedEvents.g.cs", sourceCode);
        });
    }
}

public class GeneratedEventWithOnEvents
{
    public IMethodSymbol GeneratedEventMethod { get; }
    public string EventName { get; }
    public List<IMethodSymbol> OnEventMethods { get; }

    public GeneratedEventWithOnEvents(IMethodSymbol generatedEventMethod, string eventName, List<IMethodSymbol> onEventMethods)
    {
        GeneratedEventMethod = generatedEventMethod;
        EventName = eventName;
        OnEventMethods = onEventMethods;
    }
}

public class GeneratedCodeForClass
{
    public INamedTypeSymbol ContainingType { get; }
    public List<GeneratedEventWithOnEvents> GeneratedEventsWithOnEvents { get; }

    public GeneratedCodeForClass(INamedTypeSymbol containingType, List<GeneratedEventWithOnEvents> generatedEventsWithOnEvents)
    {
        ContainingType = containingType;
        GeneratedEventsWithOnEvents = generatedEventsWithOnEvents;
    }
}
