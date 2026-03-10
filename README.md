![plot](assets/icon.png)



# RoslynMcp

A Model Context Protocol (MCP) server that brings Roslyn code intelligence to AI agents.


## Get It on NuGet

[![NuGet](https://img.shields.io/nuget/v/RoslynMcp.svg)](https://www.nuget.org/packages/RoslynMcp/)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue)](https://www.nuget.org/packages/RoslynMcp/)

_This project uses Roslynator, licensed under Apache 2.0._

#### Installation

```bash
dotnet tool install -g RoslynMcp
```

#### Update

```bash
dotnet tool update -g RoslynMcp
```


#### MCP config (OpenCode)

```json
  "mcp": {
    "code inspection": {
      "type": "local",
      "command": [
        "roslynmcp"
      ]
    }
  }
```


## What It Is

RoslynMcp is a .NET application that exposes the power of [Roslyn](https://github.com/dotnet/roslyn) (the .NET compiler platform) through the MCP protocol. It acts as a bridge between AI assistants and your C# codebase, enabling deep code understanding and analysis.

## Why It Exists

Traditional AI code assistants often rely on simplistic pattern matching (grep/glob) which misses semantic context. RoslynMcp solves this by providing:

- **Semantic understanding** — It knows what your code *means*, not just what it *says*
- **Symbol resolution** — Understands types, methods, properties across your entire solution
- **Call graph tracing** — See how code flows through your system
- **Code smell detection** — Identifies potential issues using [Roslynator](https://github.com/dotnet/roslynator) analyzers

## What You Can Use It For

| Inspection Tool          | Description                                                        |
| ------------------------ | ------------------------------------------------------------------ |
| **Load Solution**        | Loads a .sln/.slnx file and prepare the workspace                  |
| **Understand Codebase**  | Quick orientation with complexity hotspots                         |
| **List Dependencies**    | Understand project relationships                                   |
| **List Types**           | Discover all classes, interfaces, enums in a project               |
| **List Members**         | Explore methods, properties, fields of any type                    |
| **Resolve Symbol**       | Get canonical IDs for a single code symbol                         |
| **Resolve Symbols**      | Resolve multiple symbols in one round-trip                         |
| **Explain Symbol**       | Understand what a symbol does and where it's used                  |
| **Trace Call Flow**      | See upstream callers or downstream callees                         |
| **Find Callers**         | Return only immediate direct upstream callers                      |
| **Find Callees**         | Return only immediate direct downstream callees                    |
| **Find Usages**          | Locate all references to a type/member                             |
| **Find Implementations** | Locate all implementations of a interface or abstract class/method |
| **Get Type Hierarchy**   | Explore type inheritance and derived types                         |
| **Find Code Smells**     | Detect potential issues in a file                                  |

| Mutation Tool              | Description                                                                    |
| -------------------------- | ------------------------------------------------------------------------------ |
| **Add Method**             | Add a helper or overload to an existing type using symbol-aware insertion      |
| **Replace Method**         | Replace a full method declaration when name, signature, or modifiers must move |
| **Replace Method Body**    | Change only method logic while preserving the existing declaration shape        |
| **Delete Method**          | Remove an obsolete method or disposable helper by exact symbol target          |
| **Rename Symbol**          | Rename operation for types, methods, etc.                                      |
| **Format Document**        | Format a C# source file using the solution's settings                          |

### When To Use Mutation Tools vs Text Edits

Use mutation tools when an agent needs a precise, symbol-aware edit and wants Roslyn to target the exact declaration instead of relying on file positions or text matching. They are especially useful for overloaded methods, signature changes, targeted logic replacement, and cleanup work where touching the wrong method would be expensive.

Prefer normal text edits when the task spans multiple nearby code regions, when the desired syntax is not well expressed by the mutation API, or when the agent is still shaping code quickly and does not yet need symbol-level precision.

In practice, the best workflow is often hybrid: resolve the symbol with Roslyn, use mutation tools for the risky surgical edit, and fall back to normal file editing for broader reshaping around it.



## Public MCP API

These tool descriptions are written as routing triggers. Use them to help an agent decide which tool to call based on the user's intent.


### `load_solution`

Use this tool when you need to start working with a .NET solution and no solution has been loaded yet. This must be the first tool called in a session before any code analysis or navigation tools can be used. The result now includes a readiness state so fresh or detached worktrees can be reported as degraded_missing_artifacts or degraded_restore_recommended instead of leaving users to infer that from diagnostics alone.

Parameters:
- `solutionHintPath` (optional): Absolute path to a `.sln` file, or to a directory used as the recursive discovery root for `.sln`/`.slnx` files. If omitted, the tool will auto-detect from the current workspace.


### `understand_codebase`

Use this tool when you need a quick overview of the codebase structure at the start of a session. It returns the project structure with dependency relationships and identifies hotspots from hand-written source by default so generated/intermediate artifacts do not dominate the initial view.

Parameters:
- `profile` (optional): Analysis depth. `quick` for fast results, `standard` for balanced output, `deep` for thorough analysis. Defaults to `standard`.


### `list_dependencies`

Use this tool when you need to understand how projects relate to each other within a solution. It shows the dependency graph between projects, indicating which projects depend on which others. For automation, prefer projectPath as the stable selector; projectId is snapshot-local to the active workspace snapshot.

Parameters:
- `projectPath` (optional): Exact path to a project file (`.csproj`). This is the recommended stable selector for automation. Specify only one of `projectPath`, `projectName`, or `projectId`.
- `projectName` (optional): Name of a project. Specify only one of `projectPath`, `projectName`, or `projectId`.
- `projectId` (optional): Project identifier from the current `load_solution` workspace snapshot. It is snapshot-local and can change after reload, so prefer `projectPath` for automation. Specify only one of `projectPath`, `projectName`, or `projectId`.
- `direction` (optional): Which direction of dependencies to return. `outgoing` shows what the selected project depends on. `incoming` shows what depends on the selected project. `both` returns both directions. Defaults to `both`.


### `list_types`

Use this tool when you need to list types declared in a specific loaded project. It is useful for project-scoped discovery and for finding type symbols by name before calling tools like `list_members`, `resolve_symbol`, or `get_type_hierarchy`. It can also enrich just the returned type entries with XML summaries or lightweight declared-member previews when you need a quick scan before a deeper follow-up. For automation, prefer projectPath as the stable selector; projectId is snapshot-local to the active workspace snapshot. Results prefer handwritten declarations by default and report source bias, completeness, and degraded discovery hints.

Parameters:
- `projectPath` (optional): Exact path to a project file (`.csproj`). This is the recommended stable selector for automation. Specify only one of `projectPath`, `projectName`, or `projectId`.
- `projectName` (optional): Name of a project. Specify only one of `projectPath`, `projectName`, or `projectId`.
- `projectId` (optional): Project identifier from the current `load_solution` workspace snapshot. It is snapshot-local and can change after reload, so prefer `projectPath` for automation. Specify only one of `projectPath`, `projectName`, or `projectId`.
- `namespacePrefix` (optional): Filter to only types in namespaces starting with this prefix.
- `kind` (optional): Filter by type kind: `class`, `record`, `interface`, `enum`, or `struct`.
- `accessibility` (optional): Filter by accessibility: `public`, `internal`, `protected`, `private`, `protected_internal`, or `private_protected`.
- `includeSummary` (optional): When `true`, includes XML documentation summaries for returned type entries when available. Defaults to `false`.
- `includeMembers` (optional): When `true`, includes a lightweight preview of declared members for each returned type entry on the current page. This is not full member metadata: each member is returned as a single normalized accessibility-plus-signature string, and inherited members are not included. Use `list_members` when you need detailed member data or member-level filtering. Defaults to `false`.
- `limit` (optional): Maximum number of results to return. Defaults to `100`, maximum `500`.
- `offset` (optional): Number of results to skip for pagination. Defaults to `0`.

Use `includeMembers=true` when you are still browsing candidate types and want a quick, low-detail preview inline with `list_types` results. Use `list_members` when you have chosen a specific type and need the full member listing, filtering, or inherited-member options.

Example:

```json
{
  "projectPath": "src/MyProject/MyProject.csproj",
  "namespacePrefix": "MyProject.Services",
  "includeMembers": true,
  "limit": 20
}
```


### `list_members`

Use this tool when you need to inspect the members declared by a specific type. It returns methods, properties, fields, events, and constructors, and supports filtering by kind, accessibility, binding, inheritance, and pagination so you can keep results focused.

Parameters:
- `typeSymbolId` (optional): The stable symbol ID of a type, obtained from `list_types`. Provide this OR `path`+`line`+`column`.
- `path` (optional): Path to a source file. Provide this together with `line` and `column` instead of `typeSymbolId`.
- `line` (optional): Line number (1-based) pointing to a type in the source file.
- `column` (optional): Column number (1-based) pointing to a type in the source file.
- `kind` (optional): Filter by member kind: `method`, `property`, `field`, `event`, or `ctor`.
- `accessibility` (optional): Filter by accessibility: `public`, `internal`, `protected`, `private`, `protected_internal`, or `private_protected`.
- `binding` (optional): Filter by binding type: `static` or `instance`.
- `includeInherited` (optional): When `true`, includes members from base classes. Defaults to `false`.
- `limit` (optional): Maximum number of results to return. Defaults to `100`, maximum `500`.
- `offset` (optional): Number of results to skip for pagination. Defaults to `0`.


### `resolve_symbol`

Use this tool when you have a source position (`path` + `line` + `column`), a qualified symbol name, or an existing `symbolId` and need the stable `symbolId` plus declaration location used by other navigation tools. This is often the first step before calling `explain_symbol`, `trace_call_flow`, `find_callers`, or `find_usages`. Qualified-name lookup can search the whole loaded solution, but `projectPath` is the preferred stable disambiguator for automation.

Parameters:
- `symbolId` (optional): An existing symbol ID to look up. Provide this OR `path`+`line`+`column` OR `qualifiedName`.
- `path` (optional): Path to a source file. Provide this together with `line` and `column` instead of `symbolId` or `qualifiedName`.
- `line` (optional): Line number (1-based) in the source file.
- `column` (optional): Column number (1-based) in the source file.
- `qualifiedName` (optional): A fully qualified or short type/member name (e.g., `System.String`, `MyNamespace.MyType.MyMethod`, or `MyMethod`). Provide this instead of `symbolId` or `path`+`line`+`column`.
- `projectPath` (optional): Optional project scope for `qualifiedName` lookup — path to a project that contains the symbol. This is the preferred stable selector for automation.
- `projectName` (optional): Optional project scope for `qualifiedName` lookup — name of a project that contains the symbol. Use this to narrow ambiguous matches.
- `projectId` (optional): Optional project scope for `qualifiedName` lookup — project ID from the current `load_solution` workspace snapshot. It is snapshot-local and can change after reload, so prefer `projectPath` when you need a durable selector.


### `resolve_symbols`

Use this tool when you need to resolve multiple symbols in one round-trip. Each entry reuses resolve_symbol semantics, including symbolId, source position, qualifiedName lookup, project scoping, readable symbol references, and structured ambiguity results.

Parameters:
- `entries` (required): The symbols to resolve. Each entry supports the same selector modes as resolve_symbol: symbolId, path+line+column, or qualifiedName with optional project scoping.


### `explain_symbol`

Use this tool when you need to understand what a specific symbol (type, method, property, field, etc.) does, what its signature looks like, where it is used in the codebase, and what XML documentation it already exposes. It provides a human-readable explanation along with impact hints showing areas with high reference density.

Parameters:
- `symbolId` (optional): The stable symbol ID, obtained from `resolve_symbol`, `list_types`, or `list_members`. Provide this OR `path`+`line`+`column`.
- `path` (optional): Path to a source file. Provide this together with `line` and `column` instead of `symbolId`.
- `line` (optional): Line number (1-based) pointing to the symbol in the source file.
- `column` (optional): Column number (1-based) pointing to the symbol in the source file.

The result includes a `documentation` field with:
- `summary`: The XML summary comment
- `returns`: The return value description (for methods)
- `parameters`: List of parameter names and descriptions (for methods)


### `trace_call_flow`

Use this tool when you need to understand how code flows through your system — either finding what calls a specific symbol (upstream) or what a symbol calls (downstream). This is essential for debugging, impact analysis, and understanding architectural patterns. Results prefer hand-written source by default so generated/intermediate call edges do not overwhelm interactive traces, and transition labels now degrade explicitly to unresolved_project/project_inference_degraded when attribution is uncertain. Set includePossibleTargets=true to receive a deliberate possible-runtime-target edge set for uncertain polymorphic dispatch.

Parameters:
- `symbolId` (optional): The stable symbol ID, obtained from `resolve_symbol`, `list_types`, or `list_members`. Provide this OR `path`+`line`+`column`.
- `path` (optional): Path to a source file. Provide this together with `line` and `column` instead of `symbolId`.
- `line` (optional): Line number (1-based) pointing to the symbol in the source file.
- `column` (optional): Column number (1-based) pointing to the symbol in the source file.
- `direction` (optional): Which direction to trace. `upstream` finds callers (who uses this). `downstream` finds callees (what this calls). `both` returns both directions. Defaults to `both`.
- `depth` (optional): How many levels of the call chain to traverse. Defaults to `2`. Use larger values for deeper analysis. `null` behaves the same as omitting the parameter.
- `includePossibleTargets` (optional): When true, also returns possible-runtime-target edges for uncertain interface or polymorphic dispatch. Direct static edges remain separate in the main edge list.


### `find_callers`

Use this tool when you need only the immediate direct upstream callers of a symbol. This is a focused wrapper around call-flow tracing and does not traverse beyond one caller level.

Parameters:
- `symbolId` (optional): The stable symbol ID, obtained from `resolve_symbol`, `list_types`, or `list_members`, for the symbol whose immediate direct callers you want to inspect.


### `find_callees`

Use this tool when you need only the immediate direct downstream callees of a symbol. This is a focused wrapper around call-flow tracing and does not traverse beyond one callee level.

Parameters:
- `symbolId` (optional): The stable symbol ID, obtained from `resolve_symbol`, `list_types`, or `list_members`, for the symbol whose immediate direct callees you want to inspect.


### `find_usages`

Use this tool when you need to find source-code references to a specific symbol across a document, project, or the entire solution. This is critical before refactoring or modifying a symbol to understand its static impact, but it may not include dynamic, reflection-based, or string-based usages.

Parameters:
- `symbolId` (required): The stable symbol ID, obtained from `resolve_symbol`, `list_types`, or `list_members`.
- `scope` (optional): The search scope. `project` searches only within the containing project. `solution` searches the entire solution. Defaults to `solution`.
- `path` (optional): Required when scope=document: the file path to search within.


### `find_implementations`

Use this tool when you need to find concrete implementations of an interface or abstract type, and overrides or implementations of abstract/virtual members. This is essential for understanding static polymorphic targets in the loaded solution before refactoring or changing a contract.
    
Parameters:
- `symbolId` (required): The stable symbol ID of an interface, abstract type, or abstract/virtual member, obtained from `resolve_symbol`, `list_types`, or `list_members`.


### `get_type_hierarchy`

Use this tool when you need to inspect a type's inheritance relationships: base types, implemented interfaces, and derived types. Use `includeTransitive=false` for immediate parents and children only, or `true` to expand the full transitive hierarchy.

Parameters:
- `symbolId` (required): The stable symbol ID of a type, obtained from `resolve_symbol`, `list_types`, or `list_members`. Must resolve to a type (class, interface, enum, struct, or record).
- `includeTransitive` (optional): When `true` (default), includes all transitive base types and all derived types. When `false`, returns only immediate parents and children.
- `maxDerived` (optional): Maximum number of derived types to return. Defaults to `200`. Higher values may impact performance.


### `find_codesmells`

Use this tool when you need to check a specific file for potential code quality issues. It runs Roslyn-based static analysis to detect common problems such as dead code, performance anti-patterns, naming violations, and other code smells identified by Roslynator analyzers. Optional filters operate on stable normalized risk levels (low, review_required, high, info) and categories (analyzer, correctness, design, maintainability, performance, style) in deterministic stream order. reviewMode=conservative favors stronger review signals over low-noise style and trivia suggestions.

Parameters:
- `path` (required): Path to the source file to analyze. The file must exist in the currently loaded solution.
- `maxFindings` (optional): Maximum number of accepted findings to return. When provided, discovery stops as soon as this many matching findings are found.
- `riskLevels` (optional): Accepted risk levels to include. Use normalized result values such as low, review_required, high, or info.
- `categories` (optional): Accepted categories to include. Use normalized values: analyzer, correctness, design, maintainability, performance, or style. When omitted or empty, all categories are included.
- `reviewMode` (optional): Review ranking mode. Use 'default' for the existing stream or 'conservative' to suppress lightweight style/trivia noise when stronger issues are present.


### `rename_symbol`

Use this tool when you need to rename a symbol (type, method, property, field, parameter, local variable, etc.) across the entire solution. This performs a safe refactoring that updates all references to the symbol. Returns the list of changed files.

Parameters:
- `symbolId` (required): The symbol ID of the symbol to rename. Use 'resolve_symbol' to obtain this if needed.
- `newName` (required): The new name for the symbol. Must be a valid C# identifier and should not conflict with existing symbols in the same scope.


### `add_method`

Use this tool when you need to add a new method to an existing loaded type without manually rewriting the full file. It is a good fit for adding helpers or overloads when an agent wants exact symbol-aware insertion instead of line-based text editing. Provide a target type symbol id, flat method signature fields, a list of simple parameter declaration strings like `string input`, and a body string containing only the statements inside the method body block. The body can be multi-line and may include local functions, lambdas, loops, `async`/`await`, and `try`/`catch` as long as it is valid for the requested method shape. The tool inserts the method structurally, formats the changed document, applies the solution, and returns the created method symbol id plus newly introduced diagnostics for the changed document.

Parameters:
- `targetTypeSymbolId` (required): The stable symbol id of the existing target type that should receive the new method.
- `name` (required): The new method name, for example `Evaluate`.
- `returnType` (required): The C# return type text, for example `string`, `int`, or `Task<string>`.
- `accessibility` (required): The declared accessibility: `public`, `internal`, `private`, `protected`, `protected_internal`, or `private_protected`.
- `modifiers` (optional): Optional method modifiers as individual tokens, for example `['static']` or `['async']`. Do not include accessibility here.
- `parameters` (optional): Optional parameter declarations without surrounding parentheses. Each entry should be a simple C# parameter declaration string such as `string input`, `int priority`, or `bool isEnabled`.
- `body` (required): The method body content only, without outer braces. This may contain multiple statements and nested control flow as long as the body parses as valid C# for the requested method.


### `replace_method`

Use this tool when you need to replace an existing method in a loaded type without manually rewriting the full file. This is the right choice when an agent must change the method name, signature, modifiers, or return type and wants a structural edit instead of a brittle text replacement. Provide the target method symbol id plus flat method declaration fields. Parameters must be simple C# parameter declaration strings like `string input` without surrounding parentheses, and the body must contain only the statements inside the method body without braces. The body can be multi-line and may include local functions, lambdas, loops, `async`/`await`, and `try`/`catch` as long as it is valid for the replacement method shape. The tool replaces the method structurally, formats the changed document, applies the solution, and returns the new method symbol id plus newly introduced diagnostics for the changed document. After replacement, continue with the returned new symbol id for later operations on that method.

Parameters:
- `targetMethodSymbolId` (required): The stable symbol id of the exact existing method to replace. This must resolve to one source-declared, ordinary C# method.
- `name` (required): The replacement method name, for example `Evaluate`.
- `returnType` (required): The replacement C# return type text, for example `string`, `int`, or `Task<string>`.
- `accessibility` (required): The replacement accessibility: `public`, `internal`, `private`, `protected`, `protected_internal`, or `private_protected`.
- `modifiers` (optional): Optional replacement method modifiers as individual tokens, for example `['static']` or `['async']`. Do not include accessibility here.
- `parameters` (optional): Optional replacement parameter declarations without surrounding parentheses. Each entry should be a simple C# parameter declaration string such as `string input`, `int priority`, or `bool isEnabled`.
- `body` (required): The replacement method body content only, without outer braces. This may contain multiple statements and nested control flow as long as the body parses as valid C# for the replacement method.


### `replace_method_body`

Use this tool when you need to replace only the body of an existing block-bodied method in a loaded C# solution. It is the best fit for targeted logic changes when the method declaration should stay exactly as it is. Provide the target method symbol id and a body string containing only the statements inside the method body, without outer braces. The body can be multi-line and may include local functions, lambdas, loops, `async`/`await`, and `try`/`catch` as long as it is valid for that method. The tool preserves the existing method declaration shape, replaces only the body node, formats the changed document, applies the solution, and returns changed files plus newly introduced diagnostics for the changed document. This tool currently supports only block-bodied methods, not expression-bodied methods.

Parameters:
- `targetMethodSymbolId` (required): The stable symbol id of the exact existing method whose body should be replaced. This must resolve to one source-declared, ordinary, block-bodied C# method.
- `body` (required): The replacement method body content only, without outer braces. This may contain multiple statements and nested control flow as long as the body parses as valid C# for the existing method.


### `delete_method`

Use this tool when you need to delete an existing method from a loaded solution without manually rewriting the full file. It is especially useful for exact cleanup work such as removing obsolete overloads or disposable helper methods without risking the wrong declaration in a crowded file. Provide the stable symbol id of the exact method to remove. The tool resolves the method semantically, removes its source declaration structurally, formats the changed document, applies the solution, and returns changed files plus newly introduced diagnostics for the changed document.

Parameters:
- `targetMethodSymbolId` (required): The stable symbol id of the exact existing method to delete. This must resolve to one source-declared, ordinary C# method.


### `format_document`

Use this tool when you need to format exactly one C# source file in the loaded solution using the solution's current formatting and style settings. Returns whether formatting changes were applied and persisted.

Parameters:
- `path` (required): The path to the C# source file to format. The file must be part of the currently loaded solution.
