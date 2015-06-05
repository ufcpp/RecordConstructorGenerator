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
    }
}
