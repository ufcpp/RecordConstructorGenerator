# RecordConstructorGenerator
A Code Fix for a record (immutable class/struct) constructor generated from get-only properties.

This inlcudes VSIX and NuGet packages of an analyzer created by using .NET Compiler Platform (Roslyn).

- VSIX: https://visualstudiogallery.msdn.microsoft.com/941ef3c4-a523-4d77-8bcd-fdfeebb15853
- NuGet: http://www.nuget.org/packages/RecordConstructorGenerator/

## Usage
- Insert a get-only auto property without a property initializer
- Use 'Quick Action' (Lightbulb) to fix the code

## Sample

original source:

```cs
    class Point
    {
        public string Name { get; }

        /// <summary>
        /// x coordinate.
        /// </summary>
        public int X { get; }

        /// <summary>
        /// x coordinate.
        /// </summary>
        public int Y { get; }

        public int A => X * Y;
    }
```

the generated result:

```cs
    class Point
    {
        public string Name { get; }

        /// <summary>
        /// x coordinate.
        /// </summary>
        public int X { get; }

        /// <summary>
        /// x coordinate.
        /// </summary>
        public int Y { get; }

        public int A => X * Y;

        /// <summary>Record Constructor</summary>
        /// <param name="name"><see cref="Name"/></param>
        /// <param name="x"><see cref="X"/></param>
        /// <param name="y"><see cref="Y"/></param>
        public Point(string name = default(string), int x = default(int), int y = default(int))
        {
            Name = name;
            X = x;
            Y = y;
        }
    }
```
