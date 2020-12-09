* Warn if more then one attribute for code generation applied to a type.

# StructEqualityGenerator

* Change the generator to work with classes as well.


* Warn when the attribute is applied to a non-partial struct. Code fixer?
* Warn when the attribute is used for a struct with non-mutable fields. `public X {get; private set;}` maybe is ok?
* Warn (info level diagnostics) when the attribute is used on a member tha already implements `IEquatable`.
* Warn if a struct is mutable!

* Generate Equals/GetHashCode
Use only private fields, get-only properties, properties with `get; set;` and `get; init`.
- Generic Structs (1, 2, 5 arguments)

* Skip code generation if any of the equality members are already declared.


# ToStringGenerator



Features to consider
* Configure and truncate ToString() result
* Configure and truncate a string representation of a member.

# Not supported features
* Add attributes to exclude members from ToString/Equality generated code

