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

        /// <summary>
        /// Record Constructor
        /// </summary>
        /// <param name="x"></param>
        public Point(int x = default(int))
        {
            X = x;
        }

        public int A { get { return X * Y; } }
        public int B => X * Y;
        public int C { set { } }
    }
}
