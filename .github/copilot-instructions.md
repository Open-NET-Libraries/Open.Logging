# AI Coding Directive for This Repository

This repository allows AI tools (such as GitHub Copilot, Claude, etc.) to assist with code generation, refactoring, and exploratory prototyping.

To ensure coherence, testability, and long-term maintainability, all AI-generated code must follow the architectural and stylistic guidelines below.

---

## 🧱 Architectural Philosophy

- Prefer functional, declarative flows
- Favor small, composable units of logic
- Use interfaces and dependency injection for all swappable behaviors
- Avoid hardcoded logic, especially in long methods or monolithic classes
- Use immutability and stateless design wherever possible

---

## ✅ Preferred Development Flow

When implementing any new behavior:

1. **Define the Interface First**
   - Describe what the component must do, not how
   - Ensure it's testable in isolation

2. **Write Tests Second**
   - Use realistic input data
   - Validate behavior and edge cases before implementing internals

3. **Implement Logic Last**
   - Use focused, readable methods
   - Favor clarity over cleverness

If an interface already exists, default to writing test-driven implementations in small, verifiable steps.

---

## C# Language Features

Use modern C# features like:
- `record` and `readonly record struct` for data structures.
- Collection expressions
- Cutting edge features like the `field` keyword
- Smart use of `Span<T>`, `Memory<T>`, `ReadOnlySpan<T>` and `ReadOnlyMemory<T>`
- Use `ReadOnlySpan<char>` when possible instead of strings, and if need be use `StringSegment` to minimize allocations.


## 🧪 Testing and Validation

- All logic should be covered by unit tests
- Use `NSubstitute` for mocking (never Moq)
- Use `Verify` for snapshot-based validation (XML, SVG, JSON, etc.)
- Tests must be deterministic and reproducible

---

## ⚙️ Formatting and Code Style

- Follow the rules from `.editorconfig`.
- Make sure to put an extra line after closing braces.
- Use primary constructors when possible.
- Use expressive, intention-revealing names (no abbreviations)
- Public interfaces must include XML documentation (`///`)
- Use fluent APIs where appropriate to improve readability
- Avoid static state unless explicitly required
- Do not use `async void` except for event handlers
- Use `PascalCase` for all file, folder, and type names
- Operators should be placed at the beginning of the line instead of the end for easier readability.

Example of operator placement:
```cs
public class Example
{
    public void Method()
    {
        var result = someCondition
            ? SomeMethod()
            : AnotherMethod();
    }

    public bool IsCorrect()
    {
        return someCondition
            && SomeMethod()
            || AnotherMethod();
    }
}
```

---

## 🤖 AI Behavior Expectations

- Reuse existing interfaces when present — do not redefine without cause
- Do not auto-generate boilerplate or excessive scaffolding
- Avoid modifying `.editorconfig`, `.github/`, or directives without explicit instruction
- If you're unsure about a design decision, generate a draft and ask for confirmation
- Write tests alongside any new code — test-first is encouraged
- Keep a set of development logs or "progress reports" to track changes and decisions.

---

## 🧩 Partial Class Strategy (for Large or Complex Types)

When a class or static class becomes large or contains distinct conceptual regions, prefer splitting it into **partial classes across multiple files**.

### ✳️ File Naming Convention

* **Primary file**:
  `ClassName._.cs`
  This contains the XML documentation, constructor, finalizer, disposal, and core structure.

* **Additional parts** (by concern):
  `ClassName.Category.cs`
  For example:

  * `BarMath._.cs`
  * `BarMath.Range.cs`
  * `BarMath.Transform.cs`

### ✅ Why This Matters

* Keeps each file small and focused
* Improves AI comprehension and navigation
* Avoids monolithic files and unnecessary `#region` use
* Underscore (`_`) ensures the base file sorts to the top

Partial class splitting is allowed for both instance types and static classes.

Use this pattern proactively when:

* Files exceed \~250 lines
* The class implements multiple conceptual areas (e.g., parsing, validation, formatting)

---

## 🎨 Output and Visualization

- Visual data should be exported in structured formats like XML or SVG
- Use XSLT or structured transforms to render charts when needed
- Snapshot tests for rendered output are encouraged and supported

---

## 🚧 Experimental Areas

When exploring prototypes (e.g., ML setups, classifiers, synthetic data generators):

- Stub logic using randomness or placeholders
- Keep scope narrow and functions isolated
- Add `TODO:` comments for any logic not yet confirmed

---

This file defines the baseline expectations for AI-assisted contributions.  
All generated code should reflect the quality and clarity of experienced human developers working in modern, test-driven, interface-oriented environments.
