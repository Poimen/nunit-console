// ***********************************************************************
// Copyright (c) 2016 Charlie Poole
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using System;
using System.IO;
using System.Reflection;
namespace NUnit.Engine.Internal
{
    /// <summary>
    /// AssemblyHelper provides static methods for working
    /// with assemblies.
    /// </summary>
    public static class AssemblyHelper
    {
#if NETSTANDARD1_3
        static readonly string UriSchemeFile = "file";
        static readonly string SchemeDelimiter = "://";
#else
        static readonly string UriSchemeFile = Uri.UriSchemeFile;
        static readonly string SchemeDelimiter = Uri.SchemeDelimiter;
#endif

        /// <summary>
        /// Gets the currently executing assembly in an x-plat way
        /// </summary>
        /// <returns></returns>
        public static Assembly GetExecutingAssembly()
        {
#if NETSTANDARD1_3
            return typeof(AssemblyHelper).GetTypeInfo().Assembly;
#else
            return Assembly.GetExecutingAssembly();
#endif
        }

        /// <summary>
        /// Gets the path to the directory from which an assembly was loaded.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <returns>The path.</returns>
        public static string GetDirectoryName(Assembly assembly)
        {
            return Path.GetDirectoryName(GetAssemblyPath(assembly));
        }

        /// <summary>
        /// Gets the path from which an assembly was loaded.
        /// For builds where this is not possible, returns
        /// the name of the assembly.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <returns>The path.</returns>
        public static string GetAssemblyPath(Assembly assembly)
        {
            string codeBase = assembly.CodeBase;

            if (IsFileUri(codeBase))
                return GetAssemblyPathFromCodeBase(codeBase);

            return assembly.Location;
        }

        private static bool IsFileUri(string uri)
        {
            return uri.ToLower().StartsWith(UriSchemeFile);
        }

        /// <summary>
        /// Gets the assembly path from code base.
        /// </summary>
        /// <remarks>Public for testing purposes</remarks>
        /// <param name="codeBase">The code base.</param>
        /// <returns></returns>
        public static string GetAssemblyPathFromCodeBase(string codeBase)
        {
            // Skip over the file:// part
            int start = UriSchemeFile.Length + SchemeDelimiter.Length;

            if (codeBase[start] == '/') // third slash means a local path
            {
                // Handle Windows Drive specifications
                if (codeBase[start + 2] == ':')
                    ++start;
                // else leave the last slash so path is absolute
            }
            else // It's either a Windows Drive spec or a share
            {
                if (codeBase[start + 1] != ':')
                    start -= 2; // Back up to include two slashes
            }

            return codeBase.Substring(start);
        }
    }
}
