namespace RecordConstructorGenerator.Test
{
    /// <summary>
    /// sample class.
    /// </summary>
    class Point
    {
        /// <summary>
        /// x coordinate.
        /// </summary>
        public int X { get; }

        /// <summary>
        /// x coordinate.
        /// </summary>
        public int Y { get; }

        /// <summary>Record Constructor</summary>
        /// <param name="x"><see cref="X"/></param>
        /// <param name="y"><see cref="Y"/></param>
        public Point(int x = default(int), int y = default(int))
        {
            X = x;
            Y = y;
        }

        public int A { get { return X * Y; } }
        public int B => X * Y;
        public int C { set { } }
    }
}
