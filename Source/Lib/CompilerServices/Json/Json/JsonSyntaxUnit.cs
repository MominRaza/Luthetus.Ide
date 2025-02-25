﻿using Luthetus.CompilerServices.Lang.Json.Json.SyntaxObjects;
using Luthetus.TextEditor.RazorLib.CompilerServices;

namespace Luthetus.CompilerServices.Lang.Json.Json;

public class JsonSyntaxUnit
{
    public JsonSyntaxUnit(
        JsonDocumentSyntax jsonDocumentSyntax,
        LuthDiagnosticBag diagnosticBag)
    {
        JsonDocumentSyntax = jsonDocumentSyntax;
        DiagnosticBag = diagnosticBag;
    }

    public JsonDocumentSyntax JsonDocumentSyntax { get; }
    public LuthDiagnosticBag DiagnosticBag { get; }
}