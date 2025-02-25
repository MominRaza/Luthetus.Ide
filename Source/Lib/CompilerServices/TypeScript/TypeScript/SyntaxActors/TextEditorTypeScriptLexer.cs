﻿using System.Collections.Immutable;
using Luthetus.Common.RazorLib.Keys.Models;
using Luthetus.Common.RazorLib.RenderStates.Models;
using Luthetus.CompilerServices.Lang.TypeScript.TypeScript.Facts;
using Luthetus.TextEditor.RazorLib.CompilerServices;
using Luthetus.TextEditor.RazorLib.CompilerServices.Facts;
using Luthetus.TextEditor.RazorLib.CompilerServices.GenericLexer;
using Luthetus.TextEditor.RazorLib.CompilerServices.GenericLexer.SyntaxActors;
using Luthetus.TextEditor.RazorLib.CompilerServices.Implementations;
using Luthetus.TextEditor.RazorLib.CompilerServices.Syntax.Tokens;
using Luthetus.TextEditor.RazorLib.Lexes.Models;

namespace Luthetus.CompilerServices.Lang.TypeScript.TypeScript.SyntaxActors;

public class TextEditorTypeScriptLexer : LuthLexer
{
    public static readonly GenericPreprocessorDefinition TypeScriptPreprocessorDefinition = new(
        "#",
        ImmutableArray<DeliminationExtendedSyntaxDefinition>.Empty);

    public static readonly GenericLanguageDefinition TypeScriptLanguageDefinition = new GenericLanguageDefinition(
        "\"",
        "\"",
        "(",
        ")",
        ".",
        "//",
        new[]
        {
            WhitespaceFacts.CARRIAGE_RETURN.ToString(),
            WhitespaceFacts.LINE_FEED.ToString()
        }.ToImmutableArray(),
        "/*",
        "*/",
        TypeScriptKeywords.ALL,
        TypeScriptPreprocessorDefinition);

    private readonly GenericSyntaxTree _typeScriptSyntaxTree;
    
    public TextEditorTypeScriptLexer(ResourceUri resourceUri, string sourceText)
        : base(
            resourceUri,
            sourceText,
            new LuthLexerKeywords(TypeScriptKeywords.ALL, ImmutableArray<string>.Empty, ImmutableArray<string>.Empty))
    {
        _typeScriptSyntaxTree = new GenericSyntaxTree(TypeScriptLanguageDefinition);
    }

    public Key<RenderState> ModelRenderStateKey { get; private set; } = Key<RenderState>.Empty;

    public override void Lex()
    {
        var typeScriptSyntaxUnit = _typeScriptSyntaxTree.ParseText(
            ResourceUri,
            SourceText);

        var syntaxWalker = new GenericSyntaxWalker();
        syntaxWalker.Visit(typeScriptSyntaxUnit.GenericDocumentSyntax);

        _syntaxTokenList.AddRange(
            syntaxWalker.StringSyntaxList.Select(x => new BadToken(x.TextSpan)));

        _syntaxTokenList.AddRange(
            syntaxWalker.CommentSingleLineSyntaxList.Select(x => new BadToken(x.TextSpan)));

        _syntaxTokenList.AddRange(
            syntaxWalker.CommentMultiLineSyntaxList.Select(x => new BadToken(x.TextSpan)));

        _syntaxTokenList.AddRange(
            syntaxWalker.KeywordSyntaxList.Select(x => new BadToken(x.TextSpan)));

        _syntaxTokenList.AddRange(
            syntaxWalker.FunctionSyntaxList.Select(x => new BadToken(x.TextSpan)));
    }
}