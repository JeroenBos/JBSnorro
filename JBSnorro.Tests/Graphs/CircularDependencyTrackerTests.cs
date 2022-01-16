using JBSnorro.Graphs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JBSnorro.Tests.Graphs
{
    public class CircularDependencyTrackerTests
    {
        [TestMethod]
        public void Test_Adding_Does_Not_Throw()
        {
            var tracker = new CircularDependencyTracker<int>();

            tracker.Add(0);
        }

        [TestMethod]
        public void Test_Adding_Twice_Does_Not_Throw()
        {
            var tracker = new CircularDependencyTracker<int>();

            tracker.Add(0);
            tracker.Add(1);
        }

        [TestMethod]
        public void Test_Adding_Twice_The_Same_Node_Does_Not_Throw()
        {
            var tracker = new CircularDependencyTracker<int>();

            tracker.Add(1);
            tracker.Add(1);
        }


        [TestMethod]
        public void Test_Adding_The_Same_Dependency_Twice_Does_Not_Throw()
        {
            var tracker = new CircularDependencyTracker<int>();

            tracker.Add(0, 9);
            tracker.Add(1, 9);
        }

        [TestMethod]
        public void Test_Adding_To_Double_Dependency_Does_Not_Throw()
        {
            var tracker = new CircularDependencyTracker<int>();

            tracker.Add(0, 9);
            tracker.Add(1, 9);
            tracker.Add(9, 8);
        }

        [TestMethod, ExpectedException(typeof(CircularDependencyException))]
        public void Test_Depending_On_Self_Throws()
        {
            var tracker = new CircularDependencyTracker<int>();

            // Act
            tracker.Add(0, 0);
        }

        [TestMethod, ExpectedException(typeof(CircularDependencyException))]
        public void Test_adding_binary_circle_Throws()
        {
            var tracker = new CircularDependencyTracker<int>();

            tracker.Add(0, 1);

            // Act
            tracker.Add(1, 0);
        }

        [TestMethod, ExpectedException(typeof(CircularDependencyException))]
        public void Test_adding_ternary_circle_Throws()
        {
            var tracker = new CircularDependencyTracker<int>();

            tracker.Add(0, 1);
            tracker.Add(1, 2);

            // Act
            tracker.Add(2, 0);
        }


        [TestMethod, ExpectedException(typeof(CircularDependencyException))]
        public void Test_linking_binary_circle_Throws()
        {
            var tracker = new CircularDependencyTracker<int>();

            tracker.Add(0, 1);
            tracker.Add(1, 2);

            // Act
            tracker.Add(1, 0);
        }
    }
}
