namespace RecordConstructorGenerator.Test
{
    public struct Point
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
}
