using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

// Set the orderers
[assembly: TestCollectionOrderer("XUnitTests.TestCollectionOrderer", "XUnitTests")]
[assembly: TestCaseOrderer("XUnitTests.TestCaseOrderer", "XUnitTests")]

// Need to turn off test parallelization so we can validate the run order
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace XUnitTests
{
    public static class Configuration
    {
        public static string BeeyUrl { get => "http://localhost:61497"; } 
        public static string Email { get => "milos.kudelka@newtontech.cz"; }
        public static string Password { get => "OVPgod"; }
    }
}
