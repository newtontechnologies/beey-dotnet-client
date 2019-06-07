using System;
using System.Collections.Generic;
using System.Text;

namespace XUnitTests
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    class TestPriorityAttribute : Attribute
    {
        public double Priority { get; private set; }

        public TestPriorityAttribute(double priority)
        {
            Priority = priority;
        }
    }
}
