using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DotLua.Cli
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Lua lua = new Lua();
            lua.DynamicContext.print = (LuaFunction)print;
            lua.DynamicContext.read = (LuaFunction)read;


            while (true)
            {
                string line = Console.ReadLine();
                try
                {
                    lua.DoString(line);
                }
                catch (LuaException ex)
                {
                    for (int i = 0; i < ex.col - 1; i++)
                        Console.Write(" ");

                    Console.WriteLine("^");
                    Console.WriteLine(ex.message);
                }
            }
        }

       private static LuaArguments print(LuaArguments args)
        {
            Console.WriteLine(string.Join("\t", args.Select(x => x.ToString()).ToArray()));
            return Lua.Return();
        }

        private static LuaArguments io_write(LuaArguments args)
        {
            Console.Write(args[0].ToString());
            return Lua.Return();
        }

        private static LuaArguments read(LuaArguments args)
        {
            return Lua.Return(Console.ReadLine());
        }

    }
}
