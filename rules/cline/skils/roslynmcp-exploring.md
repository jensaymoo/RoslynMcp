---
name: roslynmcp-exploring
description: Detailed exploration or analyze of types, APIs, and documentation within a specific type, namespace, or project
---

## Purpose

This skill provides a structured approach for detailed study of types, their APIs, and documentation within a specific namespace or project. It enables deep understanding of component internals before making changes or integrating with existing code. It can also be executed in plan mode, allowing step-by-step analysis, decomposition of the project structure, and iterative refinement of understanding before performing any actions

## Scope

- Studying specific module before making changes
- Understanding library's public API
- Searching for existing implementations before creating new ones

---

## CRITICAL INSTRUCTIONS

### Mandatory Requirements

1. **MUST** Always start with `load_solution`
2. **MUST** Specify **full absolute path** to the solution file (from filesystem root to `.sln`/`.slnx`)
3. **MUST** Use `projectPath` as stable selector for automation (not `projectId`)
4. **SHOULD** Check `readiness state` after loading solution
5. **SHOULD** Prefer calling tools with XML documentation included (`includeSummary: true`) for more complete context

### Prohibited Actions

- **NEVER** Call analysis tools before loading solution
- **NEVER** Use `projectId` for automation (it's snapshot-local)

---

## Tools Used

### 1. load_solution

**MANDATORY FIRST STEP**

Loads .NET solution and prepares workspace for analysis.

| Parameter | Description |
|-----------|-------------|
| `solutionHintPath` | Full absolute path to `.sln`/`.slnx` file |

**Path Examples:**
- Linux/macOS: `/home/developer/projects/MyApplication/MyApplication.sln`
- Windows: `C:\Projects\MyApplication\MyApplication.sln`

**Readiness States to Watch:**
- `degraded_missing_artifacts` — need to run `dotnet restore`
- `degraded_restore_recommended` — need to run `dotnet restore`

---

### 2. list_types

List of types (classes, interfaces, enum, struct, record) in project.

| Parameter | Description |
|-----------|-------------|
| `projectPath` | Path to `.csproj` |
| `namespacePrefix` | Filter by namespace |
| `kind` | `class` / `record` / `interface` / `enum` / `struct` |
| `accessibility` | `public` / `internal` / `protected` / `private` |
| `includeSummary` | **Include XML documentation** (SHOULD: `true`) |
| `includeMembers` | Preview of type members |
| `limit` / `offset` | Pagination (default 100, max 500) |

**XML Documentation:** SHOULD use `includeSummary: true` to get XML summary comments for types.

---

### 3. list_members

Methods, properties, fields, events, constructors of a specific type.

| Parameter | Description |
|-----------|-------------|
| `typeSymbolId` | Type ID (from `list_types` or `resolve_symbol`) |
| `path` + `line` + `column` | Alternative to typeSymbolId |
| `kind` | `method` / `property` / `field` / `event` / `ctor` |
| `accessibility` | Filter by accessibility |
| `binding` | `static` / `instance` |
| `includeInherited` | Include inherited members |
| `limit` / `offset` | Pagination |

---

### 4. explain_symbol

Explanation of symbol purpose, signature, documentation and usage.

| Parameter | Description |
|-----------|-------------|
| `symbolId` | Symbol ID |
| `path` + `line` + `column` | Alternative to symbolId |

**XML Documentation:** Automatically returns full XML documentation of the symbol:
- `summary` — symbol description
- `returns` — return value description (for methods)
- `parameters` — list of parameters with descriptions (for methods)

---

## Workflow Steps

> **IMPORTANT:** Follow steps **sequentially**. Skipping steps or changing order may lead to errors or incomplete data.

### Step 1: Load Solution

```
Tool: load_solution
Parameter: solutionHintPath = "<full_absolute_path_to_solution>"
```

**Actions:**
1. Provide full absolute path to `.sln` or `.slnx` file
2. Verify response for readiness state
3. If `degraded_missing_artifacts` or `degraded_restore_recommended` — run `dotnet restore` before proceeding

---

### Step 2: Discover Types in Target Area

```
Tool: list_types
Parameters:
  - projectPath = "<path_to_csproj>"
  - namespacePrefix = "<target_namespace>" (optional)
  - includeSummary = true
```

**Actions:**
1. Specify target project path
2. Optionally filter by namespace prefix to narrow results
3. Review returned types with their XML documentation summaries
4. Identify types of interest for deeper exploration

**Tips:**
- Use `kind` parameter to filter specific type categories (e.g., only interfaces)
- Use `accessibility` to focus on public API or internal implementation
- Use pagination for large namespaces

---

### Step 3: Explore Type Members

```
Tool: list_members
Parameters:
  - typeSymbolId = "<symbol_id_from_step_2>"
```

**Actions:**
1. Use `typeSymbolId` obtained from `list_types` results
2. Review methods, properties, fields, events, constructors
3. Identify key API surface and entry points

**Tips:**
- Filter by `kind` to focus on specific member types
- Use `binding: static` to find utility methods or factory patterns
- Enable `includeInherited: true` to see full available API

---

### Step 4: Get Detailed Symbol Documentation

```
Tool: explain_symbol
Parameter: symbolId = "<symbol_id_of_interest>"
```

**Actions:**
1. Select symbols requiring deeper understanding
2. Get full XML documentation including parameters and return values
3. Understand usage patterns and intended behavior

---

## Expected Outcomes

After completing this workflow, you will have:

1. **Type Inventory** — complete list of types in target domain with descriptions
2. **API Surface Understanding** — knowledge of available methods, properties, and their purposes
3. **Documentation Context** — XML documentation providing usage guidance
4. **Integration Points** — identified entry points for working with the component
