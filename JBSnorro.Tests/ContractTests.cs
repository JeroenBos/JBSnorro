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
        public void TestCallerExpressionOnRequires()
        {
            const bool f = false;
            try
            {
                Contract.Requires(f);
            }
            catch (Exception ex)
            {
                Contract.Assert(ex.Message == "Precondition failed: 'f'");
                return;
            }
            Contract.Throw();
        }
        [TestMethod]
        public void TestCallerExpressionOnAssert()
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
        [TestMethod]
        public void TestCallerExpressionOnEnsures()
        {
            const bool f = false;
            try
            {
                Contract.Ensures(f);
            }
            catch (Exception ex)
            {
                Contract.Assert(ex.Message == "Postcondition failed: 'f'");
                return;
            }
            Contract.Throw();
        }
    }
}
