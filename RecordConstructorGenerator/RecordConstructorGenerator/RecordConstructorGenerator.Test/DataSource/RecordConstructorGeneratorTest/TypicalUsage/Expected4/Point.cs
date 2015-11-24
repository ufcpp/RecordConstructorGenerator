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
        /// <summary>Copy Constructor</summary>
        /// <param name="name"><see cref="Name"/></param>
        /// <param name="x"><see cref="X"/></param>
        /// <param name="y"><see cref="Y"/></param>
        public Point(Point obj)
        {
            Name = obj.Name;
            X = obj.X;
            Y = obj.Y;
        }
    }
}
