---
name: roslynmcp-code-quality
description: Systematic identification and prioritization of code quality issues using static analysis
---

## Purpose

This skill provides a structured approach for systematic identification and prioritization of code quality issues using static analysis. It enables comprehensive code quality audits and helps improve project maintainability. It can also be executed in plan mode, allowing step-by-step analysis, prioritization of findings, and iterative refinement of insights before applying improvements

## Scope

- Code review and quality audit
- Identifying potential bugs and anti-patterns
- Improving project maintainability

---

## CRITICAL INSTRUCTIONS

### Mandatory Requirements

1. **MUST** Always start with `load_solution` 
2. **MUST** Specify **full absolute path** to the solution file (from filesystem root to `.sln`/`.slnx`) 
3. **MUST** Obtain `symbolId` via `resolve_symbol` before using in other tools 
4. **MUST** Use `projectPath` as stable selector for automation (not `projectId`) 
5. **SHOULD** Check `readiness state` after loading solution 

### Prohibited Actions

- **NEVER** Call analysis tools before loading solution 
- **NEVER** Assume `symbolId` — always obtain via `resolve_symbol` 
- **NEVER** Use `projectId` for automation (it's snapshot-local) 

---

## Tools Used

### 1. load_solution

**MANDATORY FIRST STEP**

Loads .NET solution and prepares workspace for analysis .

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

### 2. find_codesmells

Static file analysis via Roslynator for detecting potential problems .

| Parameter | Description |
|-----------|-------------|
| `path` | Path to `.cs` file (required) |
| `maxFindings` | Max number of results |
| `riskLevels` | `low` / `review_required` / `high` / `info` |
| `categories` | `analyzer` / `correctness` / `design` / `maintainability` / `performance` / `style` |
| `reviewMode` | `default` / `conservative` (suppresses noise) |

**Risk Levels:**
- `high` — Critical issues requiring immediate attention
- `review_required` — Issues that need human review
- `low` — Minor issues with minimal impact
- `info` — Informational findings

**Categories:**
- `correctness` — Potential bugs and logical errors
- `design` — Architectural and design problems
- `maintainability` — Code that's hard to maintain
- `performance` — Performance-related issues
- `style` — Code style violations
- `analyzer` — General analyzer findings

---

### 3. explain_symbol

Explanation of symbol purpose, signature, documentation and usage .

| Parameter | Description |
|-----------|-------------|
| `symbolId` | Symbol ID |
| `path` + `line` + `column` | Alternative to symbolId |

**XML Documentation:** Automatically returns full XML documentation of the symbol :
- `summary` — symbol description
- `returns` — return value description (for methods)
- `parameters` — list of parameters with descriptions (for methods)

---

### 4. resolve_symbol

Obtaining stable symbol identifier for use in other tools .

**Input data (one of the options):**
- `path` + `line` + `column` — position in source file
- `qualifiedName` — full or short name (e.g., `System.String`, `MyClass.MyMethod`)
- `symbolId` — existing ID for verification

| Parameter | Description |
|-----------|-------------|
| `projectPath` | Preferred stable selector |
| `projectName` | Project name for refinement |

---

### 5. find_usages

Finds all references to symbol in project or solution .

| Parameter | Description |
|-----------|-------------|
| `symbolId` | Symbol ID (required) |
| `scope` | `project` / `solution` (default) |
| `path` | Required when scope=document |

**Important:** Used for impact analysis when deciding how to address quality issues .

---

## Workflow Steps

> **IMPORTANT:** Follow steps **sequentially**. Skipping steps or changing order may lead to errors or incomplete data .

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

### Step 2: Detect Code Smells

```
Tool: find_codesmells
Parameters:
  - path = "<path_to_cs_file>"
  - reviewMode = "conservative"
```

**Actions:**
1. Analyze target `.cs` files one by one
2. Use `reviewMode: conservative` to reduce noise and focus on significant issues
3. Collect findings for prioritization

**Tips:**
- Start with core business logic files
- Analyze files modified recently for regression detection
- Use `maxFindings` to limit results for large files

---

### Step 3: Categorize and Prioritize Issues

**Actions:**
1. Group findings by `categories`:
    - **correctness** → Highest priority (potential bugs)
    - **design** → High priority (architectural issues)
    - **maintainability** → Medium priority
    - **performance** → Context-dependent priority
    - **style** → Lower priority

2. Group findings by `riskLevels`:
    - **high** → Immediate attention required
    - **review_required** → Schedule for review
    - **low** → Address during regular maintenance
    - **info** → Document for awareness

---

### Step 4: Get Context for Problematic Symbols

```
Tool: explain_symbol
Parameters:
  - path = "<file_path>"
  - line = <line_number>
  - column = <column_number>
```

**Actions:**
1. For each significant finding, get symbol context
2. Review XML documentation to understand intended behavior
3. Assess if the issue is a genuine problem or false positive

---

### Step 5: Impact Analysis for Decision Making

```
Tool: find_usages
Parameters:
  - symbolId = "<symbol_id_from_step_4>"
  - scope = "solution"
```

**Actions:**
1. For issues requiring code changes, assess impact
2. Identify all locations that would be affected by fixes
3. Prioritize fixes based on usage frequency and criticality

---

## Expected Outcomes

After completing this workflow, you will have:

1. **Quality Issue Inventory** — comprehensive list of code smells and potential problems
2. **Prioritized Action Items** — issues categorized by severity and type
3. **Context Understanding** — detailed information about problematic symbols
4. **Impact Assessment** — understanding of fix scope for each issue
