# DotLua
A Lua scripting engine written entirely in DNX (C#).

It was inspired by NetLua by frabert. Check his project out at https://github.com/frabert/NetLua


Examples
========

Using Lua from C#
-----------------

```c#
Lua lua = new Lua();
lua.DoString("a={4, b=6, [7]=10}"); // Interpreting Lua

var a = lua.Context.Get("a"); // Accessing Lua from C#
var a_b = a["b"].AsNumber();

double number = a[7]; // Automatic type coercion
```

Registering C# methods
----------------------

```c#
static LuaArguments print(LuaArguments args)
{
  string[] strings = args.Select(x => x.ToString()).ToArray(); // LuaArguments can be used as a LuaObject array
  Console.WriteLine(String.Join("\t", strings));
  return Lua.Return(); // You can use the Lua.Return helper function to return values
}

Lua lua = new Lua();
lua.Context.SetGlobal("print", (LuaFunction)print);
```

Using .NET 4.0 dynamic features
-------------------------------

```c#
dynamic lua = new Lua();
dynamic luaVariable = lua.var; // Lua.DynamicContext provides a dynamic version of Lua.Context

double a = luaVariable.numberValue; // Automatic type casting
double d = luaVariable.someFunc(a); // Automatic function arguments and result boxing / unboxing

lua.x = 5;
```

