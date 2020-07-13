using System;
using System.Text;
using System.Threading;

namespace AtCoderStreak
{
    class ConsoleUtil
    {
        public static string ReadPassword()
        {
            var sb = new StringBuilder();
            do
            {
                var ki = Console.ReadKey(true);
                switch (ki.Key)
                {
                    case ConsoleKey.Backspace:
                        if (sb.Length > 0)
                        {
                            sb.Remove(sb.Length - 1, 1);
                            Console.CursorLeft--;
                            Console.Write(' ');
                            Console.CursorLeft--;
                        }
                        break;
                    case ConsoleKey.Enter:
                        Console.WriteLine();
                        return sb.ToString();
                    default:
                        sb.Append(ki.KeyChar);
                        Console.Write('*');
                        break;
                }
            } while (true);
        }
    }
}
