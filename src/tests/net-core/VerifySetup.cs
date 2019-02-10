﻿/*
  This file is licensed to You under the Apache License, Version 2.0
  (the "License"); you may not use this file except in compliance with
  the License.  You may obtain a copy of the License at

  http://www.apache.org/licenses/LICENSE-2.0

  Unless required by applicable law or agreed to in writing, software
  distributed under the License is distributed on an "AS IS" BASIS,
  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
  See the License for the specific language governing permissions and
  limitations under the License.
*/

using NUnit.Framework;
using NUnit.Framework.Constraints;
using System.IO;

namespace Org.XmlUnit.Builder
{
    [TestFixture]
    public class VerifySetup
    {
        [Test]
        public void VerifyCurrentWorkingDirectory()
        {
            Assert.That(TestContext.CurrentContext.TestDirectory, Does.Contain("Debug") | Does.Contain("Release"));
        }

        [Test]
        public void VerifyTestResourcesDirCanBeFound()
        {
            var fullPath = Path.GetFullPath(TestResources.TESTS_DIR);
            Assert.IsTrue(Directory.Exists(fullPath),
                string.Format("Expected {0} (resolved to {1}) to exist", TestResources.TESTS_DIR, fullPath));
        }

        [Test]
        public void VerifyAnimalFileCanBeFound()
        {
            Assert.IsTrue(File.Exists(TestResources.ANIMAL_FILE),
                string.Format("Expected {0} (resolved to {1}) to exist - did you run 'git submodule update --init'?", TestResources.ANIMAL_FILE, Path.GetFullPath(TestResources.ANIMAL_FILE)));
        }
    }
}
