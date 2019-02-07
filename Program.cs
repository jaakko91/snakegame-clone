using System;

namespace SnakeGame
{
    /// MonoGame
    /// The main class.
    public static class Program
    {
        /// MonoGame
        /// The main entry point for the application.
        [STAThread]
        static void Main()
        {
            using (var game = new Game1())
                game.Run();
        }
    }
}
