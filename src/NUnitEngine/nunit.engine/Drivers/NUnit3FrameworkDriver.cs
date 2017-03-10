// ***********************************************************************
// Copyright (c) 2009-2014 Charlie Poole
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
using System.Collections.Generic;
using NUnit.Engine.Internal;
using NUnit.Engine.Extensibility;
#if NETSTANDARD1_3
using System.Reflection;
using System.Linq;
#else
using System.Runtime.Serialization;
#endif

namespace NUnit.Engine.Drivers
{
#if NETSTANDARD1_3
    /// <summary>
    /// NUnitFrameworkDriver is used by the test-runner to load and run
    /// tests using the NUnit framework assembly.
    /// </summary>
    public class NUnit3FrameworkDriver : IFrameworkDriver
    {
        private const string LOAD_MESSAGE = "Method called without loading any assemblies";
        const string INVALID_FRAMEWORK_MESSAGE = "Running tests against this version of the framework using this driver is not supported. Please update NUnit.Framework to the latest version.";

        private const string CONTROLLER_TYPE = "NUnit.Framework.Api.FrameworkController";
        private const string LOAD_METHOD = "LoadTests";
        private const string EXPLORE_METHOD = "ExploreTests";
        private const string COUNT_METHOD = "CountTests";
        private const string RUN_METHOD = "RunTests";
        private const string RUN_ASYNC_METHOD = "RunTests";
        private const string STOP_RUN_METHOD = "StopRun";

        private static readonly ILogger log = InternalTrace.GetLogger("NUnitFrameworkDriver");

        Assembly _testAssembly;
        Assembly _frameworkAssembly;
        object _frameworkController;
        Type _frameworkControllerType;

        /// <summary>
        /// An id prefix that will be passed to the test framework and used as part of the
        /// test ids created.
        /// </summary>
        public string ID { get; set; }

        public string Load(string testAssemblyPath, IDictionary<string, object> settings)
        {
            var idPrefix = string.IsNullOrEmpty(ID) ? "" : ID + "-";
            
            var assemblyPath = Path.GetFullPath(testAssemblyPath);
            var testAssembly = LoadAssembly(assemblyPath);
            var frameworkPath = Path.Combine(Path.GetDirectoryName(assemblyPath), "nunit.framework.dll");
            var framework = LoadAssembly(frameworkPath);

            _testAssembly = testAssembly;
            _frameworkAssembly = framework;

            _frameworkController = CreateObject(CONTROLLER_TYPE, _testAssembly, idPrefix, settings);
            if (_frameworkController == null)
                throw new NUnitEngineException(INVALID_FRAMEWORK_MESSAGE);

            _frameworkControllerType = _frameworkController.GetType();

            log.Info("Loading {0} - see separate log file", _testAssembly.FullName);
            return ExecuteMethod(LOAD_METHOD) as string;
        }

        /// <summary>
        /// Counts the number of test cases for the loaded test assembly
        /// </summary>
        /// <param name="filter">The XML test filter</param>
        /// <returns>The number of test cases</returns>
        public int CountTestCases(string filter)
        {
            CheckLoadWasCalled();
            object count = ExecuteMethod(COUNT_METHOD, filter);
            return count != null ? (int)count : 0;
        }

        /// <summary>
        /// Executes the tests in an assembly.
        /// </summary>
        /// <param name="callback">A callback that receives XML progress notices</param>
        /// <param name="filter">A filter that controls which tests are executed</param>
        /// <returns>An Xml string representing the result</returns>
        public string Run(ITestEventListener listener, string filter)
        {
            CheckLoadWasCalled();
            Action<string> callback = listener.OnTestEvent;
            log.Info("Running {0} - see separate log file", _testAssembly.FullName);
            return ExecuteMethod(RUN_METHOD, new[] { typeof(Action<string>), typeof(string) }, callback, filter) as string;
        }

        /// <summary>
        /// Returns information about the tests in an assembly.
        /// </summary>
        /// <param name="filter">A filter indicating which tests to include</param>
        /// <returns>An Xml string representing the tests</returns>
        public string Explore(string filter)
        {
            CheckLoadWasCalled();

            log.Info("Exploring {0} - see separate log file", _testAssembly.FullName);
            return ExecuteMethod(EXPLORE_METHOD, filter) as string;
        }

        /// <summary>
        /// Cancel the ongoing test run. If no  test is running, the call is ignored.
        /// </summary>
        /// <param name="force">If true, cancel any ongoing test threads, otherwise wait for them to complete.</param>
        public void StopRun(bool force)
        {
            ExecuteMethod(STOP_RUN_METHOD, force);
        }

        #region Helper Methods

        Assembly LoadAssembly(string filename)
        {
            var assemblyName = System.IO.Path.GetFileNameWithoutExtension(filename);
            return Assembly.Load(new AssemblyName(assemblyName));
        }

        void CheckLoadWasCalled()
        {
            if (_frameworkController == null)
                throw new InvalidOperationException(LOAD_MESSAGE);
        }

        object CreateObject(string typeName, params object[] args)
        {
            var typeinfo = _frameworkAssembly.DefinedTypes.FirstOrDefault(t => t.FullName == typeName);
            if (typeinfo == null)
            {
                log.Error("Could not find type {0}", typeName);
            }
            return Activator.CreateInstance(typeinfo.AsType(), args);
        }

        object ExecuteMethod(string methodName, params object[] args)
        {
            //var method = _frameworkControllerType.GetMethod(methodName, BindingFlags.Public);
            var method = _frameworkControllerType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
            return ExecuteMethod(method, args);
        }

        object ExecuteMethod(string methodName, Type[] ptypes, params object[] args)
        {
            var method = _frameworkControllerType.GetMethod(methodName, ptypes);
            return ExecuteMethod(method, args);
        }

        object ExecuteMethod(MethodInfo method, params object[] args)
        {
            if (method == null)
            {
                throw new NUnitEngineException(INVALID_FRAMEWORK_MESSAGE);
            }
            return method.Invoke(_frameworkController, args);
        }

        #endregion
    }
#else
    /// <summary>
    /// NUnitFrameworkDriver is used by the test-runner to load and run
    /// tests using the NUnit framework assembly.
    /// </summary>
    public class NUnit3FrameworkDriver : IFrameworkDriver
    {
        private const string NUNIT_FRAMEWORK = "nunit.framework";
        private const string LOAD_MESSAGE = "Method called without calling Load first";

        private static readonly string CONTROLLER_TYPE = "NUnit.Framework.Api.FrameworkController";
        private static readonly string LOAD_ACTION = CONTROLLER_TYPE + "+LoadTestsAction";
        private static readonly string EXPLORE_ACTION = CONTROLLER_TYPE + "+ExploreTestsAction";
        private static readonly string COUNT_ACTION = CONTROLLER_TYPE + "+CountTestsAction";
        private static readonly string RUN_ACTION = CONTROLLER_TYPE + "+RunTestsAction";
        private static readonly string STOP_RUN_ACTION = CONTROLLER_TYPE + "+StopRunAction";

        static ILogger log = InternalTrace.GetLogger("NUnitFrameworkDriver");

        AppDomain _testDomain;
        string _testAssemblyPath;

        object _frameworkController;

        /// <summary>
        /// Construct an NUnit3FrameworkDriver
        /// </summary>
        /// <param name="testDomain">The AppDomain in which to create the FrameworkController</param>
        public NUnit3FrameworkDriver(AppDomain testDomain)
        {
            _testDomain = testDomain;
        }

        public string ID { get; set; }

        /// <summary>
        /// Loads the tests in an assembly.
        /// </summary>
        /// <returns>An Xml string representing the loaded test</returns>
        public string Load(string testAssemblyPath, IDictionary<string, object> settings)
        {
            Guard.ArgumentValid(File.Exists(testAssemblyPath), "testAssemblyPath", "Framework driver constructor called with a file name that doesn't exist.");

            var idPrefix = string.IsNullOrEmpty(ID) ? "" : ID + "-";
            _testAssemblyPath = testAssemblyPath;
            try
            {
                _frameworkController = CreateObject(CONTROLLER_TYPE, testAssemblyPath, idPrefix, settings);
            }
            catch (SerializationException ex)
            {
                throw new NUnitEngineException("The NUnit 3.0 driver cannot support this test assembly. Use a platform specific runner.", ex);
            }

            CallbackHandler handler = new CallbackHandler();

            var fileName = Path.GetFileName(_testAssemblyPath);

            log.Info("Loading {0} - see separate log file", fileName);

            CreateObject(LOAD_ACTION, _frameworkController, handler);

            log.Info("Loaded {0}", fileName);

            return handler.Result;
        }

        public int CountTestCases(string filter)
        {
            CheckLoadWasCalled();

            CallbackHandler handler = new CallbackHandler();

            CreateObject(COUNT_ACTION, _frameworkController, filter, handler);

            return int.Parse(handler.Result);
        }

        /// <summary>
        /// Executes the tests in an assembly.
        /// </summary>
        /// <param name="listener">An ITestEventHandler that receives progress notices</param>
        /// <param name="filter">A filter that controls which tests are executed</param>
        /// <returns>An Xml string representing the result</returns>
        public string Run(ITestEventListener listener, string filter)
        {
            CheckLoadWasCalled();

            CallbackHandler handler = new RunTestsCallbackHandler(listener);

            log.Info("Running {0} - see separate log file", Path.GetFileName(_testAssemblyPath));
            CreateObject(RUN_ACTION, _frameworkController, filter, handler);

            return handler.Result;
        }

        /// <summary>
        /// Cancel the ongoing test run. If no  test is running, the call is ignored.
        /// </summary>
        /// <param name="force">If true, cancel any ongoing test threads, otherwise wait for them to complete.</param>
        public void StopRun(bool force)
        {
            CreateObject(STOP_RUN_ACTION, _frameworkController, force, new CallbackHandler());
        }

        /// <summary>
        /// Returns information about the tests in an assembly.
        /// </summary>
        /// <param name="filter">A filter indicating which tests to include</param>
        /// <returns>An Xml string representing the tests</returns>
        public string Explore(string filter)
        {
            CheckLoadWasCalled();

            CallbackHandler handler = new CallbackHandler();

            log.Info("Exploring {0} - see separate log file", Path.GetFileName(_testAssemblyPath));
            CreateObject(EXPLORE_ACTION, _frameworkController, filter, handler);

            return handler.Result;
        }

    #region Helper Methods

        private void CheckLoadWasCalled()
        {
            if (_frameworkController == null)
                throw new InvalidOperationException(LOAD_MESSAGE);
        }

        private object CreateObject(string typeName, params object[] args)
        {
            return _testDomain.CreateInstanceAndUnwrap(
                NUNIT_FRAMEWORK, typeName, false, 0,
#if !NET_4_0
                null, args, null, null, null );
#else
                null, args, null, null );
#endif
        }

    #endregion
    }
#endif
}
