# nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro.Tests.Properties
{
    internal class TestProject
    {
        /// <summary>
        /// Gets the full path to JBSnorro.Tests.
        /// </summary>
        public static string CurrentDirectory
        {
            get
            {
                var currentDir = new DirectoryInfo(Environment.CurrentDirectory);
                while (currentDir != null && currentDir.Name != "JBSnorro.Tests")
                    currentDir = currentDir.Parent;
                if (currentDir == null)
                    throw new Exception("Could not find current directory");

                return currentDir.FullName;
            }
        }
    }
}
