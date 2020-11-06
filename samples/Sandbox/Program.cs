using System;
using TestLib;

namespace Sandbox
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("huhu");
            double x = 1.0;
            var y = 2.5 * x;
            var (a, b) = GetTuple();
            Console.WriteLine($"{a} = {b}");

            var t = new TestClass();
            var tup = t.GetTuple();
            var (_, s) = tup;
            Console.WriteLine(s);

            Console.WriteLine(GetNullableDate(false));
        }

        private static (double, string) GetTuple()
        {
            return (1.2, "huhu");
        }

        private static DateTime GetDate() => DateTime.Now;

        private static DateTime? GetNullableDate(bool x) => x ? (DateTime?)null : GetDate();
    }
}
