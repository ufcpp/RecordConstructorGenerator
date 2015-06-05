namespace RecordConstructorGenerator.Test
{
    public struct Point
    {
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
        /// <param name="x"><see cref="X"/></param>
        /// <param name="y"><see cref="Y"/></param>
        public Point(int x = default(int), int y = default(int))
        {
            X = x;
            Y = y;
        }
    }
}
