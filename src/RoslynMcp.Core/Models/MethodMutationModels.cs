namespace RoslynMcp.Core.Models;

public sealed record AddMethodRequest(
    string TargetTypeSymbolId,
    MethodInsertionSpec Method);

public sealed record MethodInsertionSpec(
    string Name,
    string ReturnType,
    string Accessibility,
    IReadOnlyList<string> Modifiers,
    IReadOnlyList<MethodParameterSpec> Parameters,
    string Body,
    string? Placement = null);

public sealed record MethodParameterSpec(
    string Name,
    string Type);

public sealed record AddedMethodInfo(
    string SymbolId,
    string Signature);

public sealed record MutationDiagnosticInfo(
    string Id,
    string Severity,
    string Message,
    string FilePath,
    int Line,
    int Column,
    string? Origin = null);

public sealed record DiagnosticsDeltaInfo(
    IReadOnlyList<MutationDiagnosticInfo> NewErrors,
    IReadOnlyList<MutationDiagnosticInfo> NewWarnings);

public sealed record AddMethodResult(
    string Status,
    IReadOnlyList<string> ChangedFiles,
    string TargetTypeSymbolId,
    AddedMethodInfo? AddedMethod,
    DiagnosticsDeltaInfo DiagnosticsDelta,
    ErrorInfo? Error = null);

public sealed record DeleteMethodRequest(string TargetMethodSymbolId);

public sealed record DeletedMethodInfo(
    string SymbolId,
    string Signature);

public sealed record DeleteMethodResult(
    string Status,
    IReadOnlyList<string> ChangedFiles,
    string TargetMethodSymbolId,
    DeletedMethodInfo? DeletedMethod,
    DiagnosticsDeltaInfo DiagnosticsDelta,
    ErrorInfo? Error = null);

public sealed record ReplaceMethodRequest(
    string TargetMethodSymbolId,
    MethodInsertionSpec Method);

public sealed record ReplacedMethodInfo(
    string OriginalSymbolId,
    string OriginalSignature,
    string NewSymbolId,
    string NewSignature);

public sealed record ReplaceMethodResult(
    string Status,
    IReadOnlyList<string> ChangedFiles,
    string TargetMethodSymbolId,
    ReplacedMethodInfo? ReplacedMethod,
    DiagnosticsDeltaInfo DiagnosticsDelta,
    ErrorInfo? Error = null);

public sealed record ReplaceMethodBodyRequest(
    string TargetMethodSymbolId,
    string Body);

public sealed record ReplacedMethodBodyInfo(
    string MethodSymbolId,
    string Signature);

public sealed record ReplaceMethodBodyResult(
    string Status,
    IReadOnlyList<string> ChangedFiles,
    string TargetMethodSymbolId,
    ReplacedMethodBodyInfo? ReplacedMethodBody,
    DiagnosticsDeltaInfo DiagnosticsDelta,
    ErrorInfo? Error = null);
