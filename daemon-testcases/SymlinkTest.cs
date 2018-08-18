using System;
using NUnit.Framework;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Symlink;

namespace daemon_testcases
{
    [TestFixture]
    public class SymlinkTest
    {
        [Test]
        // Example function to test if the Windows API for Symlink Creation is working.
        public void WindowsSymlinkExample()
        {
            if (AppConfig.isWindows)
            {
                var symlinkLib = new Symlink();
                if (symlinkLib.Environment.Create("C:/Users/Public/Testing", "C:/Users/Public/"))
                {
                    // Symlink creation was successful - delete it and pass.
                    symlinkLib.Environment.Delete("C:/Users/Public/Testing");
                    Assert.Pass();
                }
                else
                {
                    // Symlink failed to be created
                    Assert.Fail();
                }
            }
            Assert.Pass("Linux Detected - Asserting as true for this test case.");
        }
    }
}