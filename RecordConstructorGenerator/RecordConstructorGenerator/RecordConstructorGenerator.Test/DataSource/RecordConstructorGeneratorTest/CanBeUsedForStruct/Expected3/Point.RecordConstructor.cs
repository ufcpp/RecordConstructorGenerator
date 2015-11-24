namespace RecordConstructorGenerator.Test
{
    partial struct Point
    {
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
}
