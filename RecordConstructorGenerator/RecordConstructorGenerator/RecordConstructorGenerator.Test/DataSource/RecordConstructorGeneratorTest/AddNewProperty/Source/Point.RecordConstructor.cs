namespace RecordConstructorGenerator.Test
{
    partial class Point
    {
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
