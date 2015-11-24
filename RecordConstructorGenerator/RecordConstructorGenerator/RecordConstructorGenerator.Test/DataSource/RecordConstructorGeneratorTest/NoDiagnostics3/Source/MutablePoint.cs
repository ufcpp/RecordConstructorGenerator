namespace RecordConstructorGenerator.Test
{
    class Point2
    {
        public int X { get; set; } = 0;

        public int Y { get; set; }

        public Point2()
        {
            Y = 0;
        }
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
