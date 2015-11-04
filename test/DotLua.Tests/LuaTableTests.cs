using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DotLua;
using Xunit;

namespace DotLua.Tests
{
    public class LuaTableTests
    {
        [Fact]
        public void FirstTableTest()
        {
            var lua = new Lua();
            lua.DoString(
@" a={}
k = 'x'
a[k] = 10        
a[20] = 'great'");
            LuaObject obj1 = 10;
            LuaObject obj2 = lua.DoString("return a['x']")[0];
            Assert.True(obj1.Equals(obj2));

            lua.DoString("k = 20");
            obj1 = "great";
            obj2 = lua.DoString("return a[k]")[0];
            Assert.True(obj1.Equals(obj2));
        }

        [Fact]
        public void SecondTableTest()
        {
            var lua = new Lua();
            lua.DoString(
@" a={}
a['x'] = 10        
b = a");
            LuaObject obj1 = 10;
            LuaObject obj2 = lua.DoString("return b['x']")[0];
            Assert.True(obj1.Equals(obj2));

            lua.DoString("b['x'] = 20");
            obj1 = 20;
            obj2 = lua.DoString("return a['x']")[0];
            Assert.True(obj1.Equals(obj2));
        }
    }
}
