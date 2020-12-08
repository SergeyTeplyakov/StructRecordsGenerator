# StructEqualityGenerator

* Warn when the attribute is applied to a non-partial struct. Code fixer?
* Warn when the attribute is used for a struct with non-mutable fields. `public X {get; private set;}` maybe is ok?
* Warn (info level diagnostics) when the attribute is used on a member tha already implements `IEquatable`.
* Warn if a struct is mutable!

* Generate Equals/GetHashCode
Use only private fields, get-only properties, properties with `get; set;` and `get; init`.
- Generic Structs (1, 2, 5 arguments)

* Skip code generation if any of the equality members are already declared.

