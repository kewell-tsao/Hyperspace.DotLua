using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DotLua.Libraries
{
    public static class IoLibrary
    {
        private static readonly dynamic FileMetatable = LuaObject.NewTable();
        private static LuaObject currentInput = LuaObject.Nil, currentOutput = LuaObject.Nil;

        private static bool isStream(LuaObject obj)
        {
            return (obj.IsUserData && obj.luaobj is FileObject);
        }

        private static LuaObject CreateFileObject(Stream stream)
        {
            var obj = LuaObject.FromObject(new FileObject(stream));
            obj.Metatable = FileMetatable;

            return obj;
        }

        private static LuaObject CreateFileObject(Stream stream, bool autoflush)
        {
            var fobj = new FileObject(stream);
            fobj.writer.AutoFlush = autoflush;
            var obj = LuaObject.FromObject(fobj);
            obj.Metatable = FileMetatable;

            return obj;
        }

        public static void AddIoLibrary(LuaContext Context)
        {
            dynamic io = LuaObject.NewTable();

            FileMetatable.__index = LuaObject.NewTable();
            FileMetatable.__index.write = (LuaFunction) write;
            FileMetatable.__index.close = (LuaFunction) close;
            FileMetatable.__index.flush = (LuaFunction) flush;
            FileMetatable.__index.seek = (LuaFunction) seek;
            FileMetatable.__index.read = (LuaFunction) read;

            io.open = (LuaFunction) io_open;
            io.type = (LuaFunction) io_type;
            io.input = (LuaFunction) io_input;
            io.output = (LuaFunction) io_output;
            io.temp = (LuaFunction) io_temp;
            io.flush = (LuaFunction) io_flush;
            io.write = (LuaFunction) io_write;
            io.read = (LuaFunction) io_read;

            currentInput = CreateFileObject(Console.OpenStandardInput());
            currentOutput = CreateFileObject(Console.OpenStandardOutput(), true);
            io.stdin = currentInput;
            io.stdout = currentOutput;
            io.stderr = CreateFileObject(Console.OpenStandardError(), true);

            Context.Set("io", io);
        }

        private static LuaArguments io_open(LuaArguments args)
        {
            var file = args[0];
            var mode = args[1];

            if (file.IsString)
            {
                var fmode = FileMode.Open;
                var faccess = FileAccess.Read;
                if (mode.IsString)
                {
                    switch (mode.ToString())
                    {
                        case "r":
                            faccess = FileAccess.Read;
                            break;
                        case "w":
                            fmode = FileMode.Create;
                            faccess = FileAccess.ReadWrite;
                            break;
                        case "a":
                            fmode = FileMode.Append;
                            faccess = FileAccess.Write;
                            break;
                        case "r+":
                        case "w+":
                        case "a+":
                            // TODO: Implement rwa+
                            throw new NotImplementedException();
                    }
                }
                var stream = new FileStream(file.ToString(), fmode, faccess);

                return Lua.Return(CreateFileObject(stream));
            }
            return Lua.Return();
        }

        private static LuaArguments io_type(LuaArguments args)
        {
            var obj = args[0];
            if (isStream(obj))
            {
                var fobj = obj.luaobj as FileObject;
                if (!fobj.stream.CanWrite && !fobj.stream.CanRead)
                    return Lua.Return("closed file");
                return Lua.Return("file");
            }
            return Lua.Return();
        }

        private static LuaArguments io_input(LuaArguments args)
        {
            var obj = args[0];
            if (isStream(obj))
            {
                currentInput = obj;
                return Lua.Return(currentInput);
            }
            if (obj.IsString)
            {
                currentInput = io_open(args)[0];
                return Lua.Return(currentInput);
            }
            if (args.Length == 0)
                return Lua.Return(currentInput);
            throw new LuaException("Invalid argument");
        }

        private static LuaArguments io_output(LuaArguments args)
        {
            var obj = args[0];
            if (isStream(obj))
            {
                currentOutput = obj;
                return Lua.Return(currentOutput);
            }
            if (obj.IsString)
            {
                var stream = new FileStream(obj.ToString(), FileMode.OpenOrCreate);
                currentOutput = CreateFileObject(stream);
                return Lua.Return(currentOutput);
            }
            if (args.Length == 0)
                return Lua.Return(currentOutput);
            throw new LuaException("Invalid argument");
        }

        private static LuaArguments io_temp(LuaArguments args)
        {
            var path = Path.GetTempFileName();
            Stream s = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Write, short.MaxValue,
                FileOptions.DeleteOnClose);

            return Lua.Return(CreateFileObject(s));
        }

        private static LuaArguments io_write(LuaArguments args)
        {
            var obj = args[0];
            if (!obj.IsNil)
                return currentOutput["write"].MethodCall(currentOutput, args);
            return Lua.Return();
        }

        private static LuaArguments io_flush(LuaArguments args)
        {
            var obj = args[0];
            if (obj.IsNil)
                return currentOutput["flush"].MethodCall(currentOutput, args);
            return obj["flush"].MethodCall(obj, args);
        }

        private static LuaArguments io_close(LuaArguments args)
        {
            var obj = args[0];
            if (obj.IsNil)
                return currentOutput["close"].MethodCall(currentOutput, args);
            return obj["close"].MethodCall(obj, args);
        }

        private static LuaArguments io_read(LuaArguments args)
        {
            return currentInput["read"].MethodCall(currentInput, args);
        }

        private static LuaArguments write(LuaArguments args)
        {
            var self = args[0];
            if (isStream(self))
            {
                var fobj = self.luaobj as FileObject;
                foreach (var arg in args)
                {
                    if (arg == self)
                        continue;

                    if (!(arg.IsString || arg.IsNumber))
                        Lua.Return();

                    if (fobj.stream.CanWrite)
                        fobj.writer.Write(arg.ToString());
                    else
                        Lua.Return();
                }
                return Lua.Return(self);
            }
            return Lua.Return();
        }

        private static LuaArguments close(LuaArguments args)
        {
            var obj = args[0];
            if (isStream(obj))
            {
                var fobj = obj.luaobj as FileObject;
#if DNXCORE50
                fobj.stream.Dispose();
#else
                fobj.stream.Close();
#endif
            }
            return Lua.Return();
        }

        private static LuaArguments flush(LuaArguments args)
        {
            var obj = args[0];
            if (isStream(obj))
            {
                var fobj = obj.luaobj as FileObject;
                fobj.writer.Flush();
            }
            return Lua.Return();
        }

        private static LuaArguments seek(LuaArguments args)
        {
            var obj = args[0];
            var whence = args[1] | "cur";
            var offset = args[2] | 0;

            if (isStream(obj))
            {
                var fobj = obj.luaobj as FileObject;
                switch (whence.ToString())
                {
                    case "cur":
                        fobj.stream.Position += (long) offset;
                        break;
                    case "set":
                        fobj.stream.Position = (long) offset;
                        break;
                    case "end":
                        fobj.stream.Position = fobj.stream.Length + (long) offset;
                        break;
                }
                return Lua.Return(fobj.stream.Position);
            }
            return Lua.Return();
        }

        private static LuaArguments read(LuaArguments args)
        {
            var self = args[0];
            if (isStream(self))
            {
                var fobj = self.luaobj as FileObject;
                if (args.Length == 1)
                {
                    var line = fobj.reader.ReadLine();

                    return Lua.Return(line);
                }
                var ret = new List<LuaObject>();
                foreach (var arg in args)
                {
                    if (arg == self)
                        continue;
                    if (arg.IsNumber)
                    {
                        var bld = new StringBuilder();
                        for (var i = 0; i < arg; i++)
                        {
                            bld.Append((char) fobj.reader.Read());
                        }
                        ret.Add(bld.ToString());
                    }
                    else if (arg == "*a")
                        ret.Add(fobj.reader.ReadToEnd());
                    else if (arg == "*l")
                        ret.Add(fobj.reader.ReadLine());
                    else if (arg == "*n")
                    {
                        //TODO: Implement io.read("*n")
                        throw new NotImplementedException();
                    }
                }
                return Lua.Return(ret.ToArray());
            }
            return Lua.Return();
        }

        // TODO: implment lines, read
        private class FileObject
        {
            public readonly StreamReader reader;
            public readonly Stream stream;
            public readonly StreamWriter writer;

            public FileObject(Stream s)
            {
                stream = s;
                if (s.CanRead)
                    reader = new StreamReader(s);
                if (s.CanWrite)
                    writer = new StreamWriter(s);
            }
        }
    }
}