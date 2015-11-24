namespace RecordConstructorGenerator.Test
{
    public class Point
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
        public Point(string name, int x, int y)
        {
            Name = name;
            X = x;
            Y = y;
        }
    }
}
