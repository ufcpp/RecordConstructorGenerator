namespace RecordConstructorGenerator.Test
{
    class Point2
    {
        public int X { get; set; }

        public int Y { get; set; }
    }

    class Point3
    {
        public int X { get; private set; }

        public int Y { get; private set; }

        public Point3()
        {
            X = 0;
            Y = 0;
        }
    }
}
