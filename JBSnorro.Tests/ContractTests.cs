using JBSnorro.Collections;
using JBSnorro.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro
{
    [TestClass]
    public class ContractTests
    {
        [TestMethod]
        public void TestCallerExpression()
        {
            const bool f = false;
            try
            {
                Contract.Assert(f);
            }
            catch (Exception ex)
            {
                Contract.Assert(ex.Message == "Assertion failed: 'f'");
                return;
            }
            Contract.Throw();
        }
    }
}
