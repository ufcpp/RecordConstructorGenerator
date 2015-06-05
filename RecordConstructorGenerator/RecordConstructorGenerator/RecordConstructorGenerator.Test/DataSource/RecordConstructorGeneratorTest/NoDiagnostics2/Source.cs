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

        #region Record Constructor

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        public Point(int x = default(int))
        {
            X = x;
            Y = x;
        }

        #endregion

        public int A { get { return X * Y; } }
        public int B => X * Y;
        public int C { set { } }
    }
}
