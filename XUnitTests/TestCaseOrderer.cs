using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace XUnitTests
{
    class TestCaseOrderer : ITestCaseOrderer
    {
        public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases) where TTestCase : ITestCase
        {
            return testCases.OrderBy(t => GetPriority(t));
        }

        private static double GetPriority(ITestCase testCase)
        {
            var priorityAttribute = testCase.TestMethod.Method
                .GetCustomAttributes(typeof(TestPriorityAttribute))
                .FirstOrDefault();

            return priorityAttribute == null
                ? 0
                : priorityAttribute.GetNamedArgument<double>("Priority");
        }
    }
}
