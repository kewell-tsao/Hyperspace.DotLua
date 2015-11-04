using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DotLua;
using Xunit;

namespace DotLua.Tests
{
    public class LuaObjectTests
    {
        [Fact]
        public void ObjectEqualityNumber()
        {
            LuaObject obj1 = 10;
            LuaObject obj2 = 10;

            Assert.True(obj1.Equals(obj2));
            Assert.True(obj2.Equals(obj1));
        }

        [Fact]
        public void ObjectEqualityString()
        {
            LuaObject obj1 = "test";
            LuaObject obj2 = "test";

            Assert.True(obj1.Equals(obj2));
            Assert.True(obj2.Equals(obj1));
        }

        [Fact]
        public void ObjectEqualityCoercion()
        {
            LuaObject obj1 = "10";
            LuaObject obj2 = 10;

            Assert.False(obj1.Equals(obj2));
            Assert.False(obj2.Equals(obj1));
        }

        [Fact]
        public void GeneralEquality()
        {
            LuaObject a = "test";

            Assert.True(a == "test");
        }

        [Fact]
        public void LogicalOperators()
        {
            LuaObject a = "test";
            LuaObject b = LuaObject.Nil;

            Assert.True((a | b) == a);
            Assert.True((a | null) == a);

            Assert.True((a & b) == b);
            Assert.True((a & null) == LuaObject.Nil);
        }
    }
}
