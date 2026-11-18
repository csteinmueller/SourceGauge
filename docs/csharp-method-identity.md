# C# method identity

For schema version 1, the C# backend sets `entity.id` for a method or constructor to its Roslyn XML documentation comment ID with the leading `M:` removed. The identifier is a case-sensitive string. It identifies the declaration, not a call site or source location; parameter names and return types are not encoded.

The following examples show the exact `entity.id` values:

| Declaration | `entity.id` |
|---|---|
| `void Run()` in `N.Example` | `N.Example.Run` |
| `void Run(int value)` in `N.Example` | `N.Example.Run(System.Int32)` |
| `void Run(string value)` overload in `N.Example` | `N.Example.Run(System.String)` |
| `Example()` constructor in `N.Example` | `N.Example.#ctor` |
| `void Map<T>(T value)` in `N.Example` | ```N.Example.Map``1(``0)``` |
| `void Accept(List<T> values)` in `N.Box<T>` | ``N.Box`1.Accept(System.Collections.Generic.List{`0})`` |
| `void Use(T outer, U inner)` in `N.Outer<T>.Inner<U>` | ``N.Outer`1.Inner`1.Use(`0,`1)`` |
| `void Change(ref int value)` in `N.Example` | `N.Example.Change(System.Int32@)` |

Overloads differ by their ordered parameter types and by-reference markers. Type parameters on generic types use single-backtick numbering in the containing type name; method type parameters use double-backtick numbering. Constructed generic parameter types use braces around their type arguments. Nested type names are dot-separated. A parameterless method has no trailing parentheses.

The C# backend normalizes partial method declarations to one method identity. `entity.assemblyName` contains the assembly's evaluated name for the project and target-framework context; it is not derived from the project filename. Consumers matching compiled methods should compare the assembly name and method ID exactly and should also retain `entity.projectId` and target-framework context to distinguish project analyses.

The identity syntax is part of the C# version 1 output contract. A future incompatible identity format requires an explicit contract-version decision. Roslyn XML documentation ID syntax is used so generic arity, overloads, constructors, nested types, and by-reference parameters have a defined representation instead of relying on display names.
