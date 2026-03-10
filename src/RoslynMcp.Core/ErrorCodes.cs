namespace RoslynMcp.Core;

public static class ErrorCodes
{
    public const string InvalidInput = "invalid_input";
    public const string InvalidRequest = "invalid_request";
    public const string InvalidMethodSpecification = "invalid_method_specification";
    public const string SolutionNotSelected = "solution_not_selected";
    public const string SolutionNotFound = "solution_not_found";
    public const string InvalidPath = "invalid_path";
    public const string PathOutOfScope = "path_out_of_scope";
    public const string SymbolNotFound = "symbol_not_found";
    public const string AmbiguousSymbol = "ambiguous_symbol";
    public const string TargetNotSourceEditable = "target_not_source_editable";
    public const string InvalidNewName = "invalid_new_name";
    public const string RenameConflict = "rename_conflict";
    public const string MethodConflict = "method_conflict";
    public const string UnsupportedSymbolKind = "unsupported_symbol_kind";
    public const string FixNotFound = "fix_not_found";
    public const string ActionNotFound = "action_not_found";
    public const string WorkspaceChanged = "workspace_changed";
    public const string StaleWorkspaceSnapshot = "stale_workspace_snapshot";
    public const string FixConflict = "fix_conflict";
    public const string PolicyBlocked = "policy_blocked";
    public const string AnalysisFailed = "analysis_failed";
    public const string CreatedSymbolUnresolved = "created_symbol_unresolved";
    public const string InternalError = "internal_error";
}
