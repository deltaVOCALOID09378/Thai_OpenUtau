#pragma warning disable CS0618, CS0649, CS8632, CS0108
using System;
using System.Text.RegularExpressions;

namespace OpenUtau.Plugin.Builtin.EnunuOnnx.nnmnkwii.python {
    public class AssertionError : Exception {

        public AssertionError() : base() { }

        public AssertionError(string message) : base(message) {
        }

        public AssertionError(string message, Exception innerException)
            : base(message, innerException) {
        }
    }

    public static class PythonAssert {
        public static void Assert(bool expression) {
            if (!expression) {
                throw new AssertionError();
            }
        }

        public static void Assert(bool expression, string message) {
            if (!expression) {
                throw new AssertionError(message);
            }
        }
    }

    //python string methods that C# doesn't have
    public static class PythonString {
        //python "".split() without parameter
        public static string[] split(string str) {
            return Regex.Split(str, @" +");
        }
    }
}
