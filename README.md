# An overview
This project contains a set of Roslyn generators helping dealing with structs in C#.

# StructRecord generator
Currently in C# 9 the records are reference types. This is fine, but in some cases one may want having a similar behavior for structs, i.e. a set of 'WithMember' methods (just because the `with` syntax is a language feature), overriden `Equals`, `GetHashCode` and `ToString` members.

To achieve this, you may mark a struct with `[StructGenerators.StructRecord]` attribute to get roughly the same behavior as records in C# 9 but for structs:

```csharp
[StructGenerators.StructRecord]
public readonly partial struct MyStruct
{
    public readonly double X;
    public readonly int Y;
    public readonly int Z;
    static readonly string _staticProperty;
}
```

In this case, the generator will generate a partial definition with the following members:

```csharp
partial struct MyStruct : IEquatable<MyStruct>
{
    public MyStruct WithX(double value) {}
    public MyStruct WithY(int value) {}
    public MyStruct WithZ(int value) {}
    public MyStruct Clone() {}
    
    // Equality members: GetHashCode, Equals(object), Equals(MyStruct)
    // operator==, operator !=
    
    public override ToString() { /* Prints all the members.*/ }
}
```

The next set of generators are used by the StructRecord generator, but can be used separately if needed.

* **StructEquality generator**. Mark your struct with `StructGenerators.StructEquality` to generate equality members based on the struct's state (all the fields and automatic properties backed by a field).
* **GenerateToString generator**. Mark your struct or class with `StructGenerators.GenerateToString` to generate `ToString` based on the type's state (including instance computed properties as well).

# Diagnostics
* **SRG001** (Warning) is emitted if the type is marked with one of the attributes but the type is not partial. In this case the generator just can't generate the partial definition.
* **SRG002** (Info) is emitted for each "generatable" member that already defined by the user.


# Example
![Demo](docs/images/StructRecordExample.gif "Demo")

# Other work
TBD