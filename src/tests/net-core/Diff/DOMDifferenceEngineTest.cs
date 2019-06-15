/*
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
using System;
using System.Linq;
using System.Xml;
using NUnit.Framework;
using Org.XmlUnit.Builder;
using Org.XmlUnit.Input;
using InputBuilder = Org.XmlUnit.Builder.Input;

namespace Org.XmlUnit.Diff {

    [TestFixture]
    public class DOMDifferenceEngineTest : AbstractDifferenceEngineTest {

        protected override AbstractDifferenceEngine DifferenceEngine {
            get {
                return new DOMDifferenceEngine();
            }
        }

        private class DiffExpecter {
            internal int invoked = 0;
            private readonly int expectedInvocations;
            private readonly ComparisonType type;
            private readonly bool withXPath;
            private readonly string controlXPath;
            private readonly string testXPath;
            private bool withParentXPath;
            private string controlParentXPath;
            private string testParentXPath;

            internal DiffExpecter(ComparisonType type) : this(type, 1) { }

            internal DiffExpecter(ComparisonType type, int expected)
                : this(type, expected, false, null, null) { }

            internal DiffExpecter(ComparisonType type, string controlXPath,
                                  string testXPath)
                : this(type, 1, true, controlXPath, testXPath) { }

            private DiffExpecter(ComparisonType type, int expected,
                                 bool withXPath, string controlXPath,
                                 string testXPath) {
                this.type = type;
                this.expectedInvocations = expected;
                this.withXPath = withXPath;
                this.controlXPath = controlXPath;
                this.testXPath = testXPath;
                withParentXPath = withXPath;
                controlParentXPath = GetParentXPath(controlXPath);
                testParentXPath = GetParentXPath(testXPath);
            }

            internal DiffExpecter WithParentXPath(string controlParentXPath, string testParentXPath) {
                withParentXPath = true;
                this.controlParentXPath = controlParentXPath;
                this.testParentXPath = testParentXPath;
                return this;
            }

            public void ComparisonPerformed(Comparison comparison,
                                            ComparisonResult outcome) {
                Assert.Greater(expectedInvocations, invoked);
                invoked++;
                Assert.AreEqual(type, comparison.Type);
                Assert.AreEqual(ComparisonResult.DIFFERENT, outcome);
                if (withXPath) {
                    Assert.AreEqual(controlXPath,
                                    comparison.ControlDetails.XPath,
                                    "Control XPath");
                    Assert.AreEqual(testXPath,
                                    comparison.TestDetails.XPath,
                                    "Test XPath");
                }
                if (withParentXPath) {
                    Assert.AreEqual(controlParentXPath,
                                    comparison.ControlDetails.ParentXPath,
                                    "Control Parent XPath");
                    Assert.AreEqual(testParentXPath,
                                    comparison.TestDetails.ParentXPath,
                                    "Test Parent XPath");
                }
            }

            internal string GetParentXPath(string xPath) {
                if (xPath == null) {
                    return null;
                }
                if (xPath == "/" || string.IsNullOrEmpty(xPath)) {
                    return string.Empty;
                }
                int i = xPath.LastIndexOf('/');
                if (i == xPath.IndexOf('/')) {
                    return "/";
                }
                return i >= 0 ? xPath.Substring(0, i) : xPath;
            }
        }

        private XmlDocument doc;

        [SetUp]
        public void CreateDoc() {
            doc = new XmlDocument();
        }

        [Test]
        public void DiffExpecterParentXPath() {
            DiffExpecter ex = new DiffExpecter(ComparisonType.ATTR_NAME_LOOKUP);
            Assert.AreEqual("/bla/blubb", ex.GetParentXPath("/bla/blubb/x[1]"));
            Assert.AreEqual("/bla/blubb", ex.GetParentXPath("/bla/blubb/@attr"));
            Assert.AreEqual("/", ex.GetParentXPath("/bla[1]"));
            Assert.AreEqual("/", ex.GetParentXPath("/@attr"));
            Assert.AreEqual(string.Empty, ex.GetParentXPath("/"));
            Assert.AreEqual(string.Empty, ex.GetParentXPath(string.Empty));
            Assert.AreEqual(null, ex.GetParentXPath(null));
        }

        [Test]
        public void CompareXPathOfDifferentRootElements() {
            DOMDifferenceEngine d = new DOMDifferenceEngine();
            DiffExpecter ex = new DiffExpecter(ComparisonType.ELEMENT_TAG_NAME,
                                               "/x[1]", "/y[1]");
            d.DifferenceListener += ex.ComparisonPerformed;
            d.ComparisonController = ComparisonControllers.StopWhenDifferent;
            d.Compare(new DOMSource(doc.CreateElement("x")),
                      new DOMSource(doc.CreateElement("y")));
            Assert.AreEqual(1, ex.invoked);
        }

        [Test]
        public void CompareNodesOfDifferentType() {
            DOMDifferenceEngine d = new DOMDifferenceEngine();
            DiffExpecter ex = new DiffExpecter(ComparisonType.NODE_TYPE);
            d.DifferenceListener += ex.ComparisonPerformed;
            d.ComparisonController = ComparisonControllers.StopWhenDifferent;
            Assert.AreEqual(WrapAndStop(ComparisonResult.DIFFERENT),
                            d.CompareNodes(doc.CreateElement("x"),
                                           new XPathContext(),
                                           doc.CreateComment("x"),
                                           new XPathContext()));
            Assert.AreEqual(1, ex.invoked);
        }

        [Test]
        public void CompareNodesWithoutNS() {
            DOMDifferenceEngine d = new DOMDifferenceEngine();
            d.DifferenceListener += delegate(Comparison comp,
                                             ComparisonResult r) {
                Assert.Fail("unexpected invocation");
            };
            d.ComparisonController = ComparisonControllers.StopWhenDifferent;
            Assert.AreEqual(Wrap(ComparisonResult.EQUAL),
                            d.CompareNodes(doc.CreateElement("x"),
                                           new XPathContext(),
                                           doc.CreateElement("x"),
                                           new XPathContext()));
        }

        [Test]
        public void CompareNodesDifferentNS() {
            DOMDifferenceEngine d = new DOMDifferenceEngine();
            DiffExpecter ex = new DiffExpecter(ComparisonType.NAMESPACE_URI);
            d.DifferenceListener += ex.ComparisonPerformed;
            d.ComparisonController = ComparisonControllers.StopWhenDifferent;
            Assert.AreEqual(WrapAndStop(ComparisonResult.DIFFERENT),
                            d.CompareNodes(doc.CreateElement("y", "x"),
                                           new XPathContext(),
                                           doc.CreateElement("y", "z"),
                                           new XPathContext()));
            Assert.AreEqual(1, ex.invoked);
        }

        [Test]
        public void CompareNodesDifferentPrefix() {
            DOMDifferenceEngine d = new DOMDifferenceEngine();
            DiffExpecter ex = new DiffExpecter(ComparisonType.NAMESPACE_PREFIX);
            d.DifferenceListener += ex.ComparisonPerformed;
            d.DifferenceEvaluator = delegate(Comparison comparison,
                                             ComparisonResult outcome) {
                if (comparison.Type == ComparisonType.NAMESPACE_PREFIX) {
                    Assert.AreEqual(ComparisonResult.DIFFERENT, outcome);
                    return ComparisonResult.DIFFERENT;
                }
                Assert.AreEqual(ComparisonResult.EQUAL, outcome);
                return ComparisonResult.EQUAL;
            };
            d.ComparisonController = ComparisonControllers.StopWhenDifferent;
            Assert.AreEqual(WrapAndStop(ComparisonResult.DIFFERENT),
                            d.CompareNodes(doc.CreateElement("x:y", "x"),
                                           new XPathContext(),
                                           doc.CreateElement("z:y", "x"),
                                           new XPathContext()));
            Assert.AreEqual(1, ex.invoked);
        }

        [Test]
        public void CompareNodesDifferentNumberOfChildren() {
            DOMDifferenceEngine d = new DOMDifferenceEngine();
            DiffExpecter ex =
                new DiffExpecter(ComparisonType.CHILD_NODELIST_LENGTH, 2);
            d.DifferenceListener += ex.ComparisonPerformed;
            d.ComparisonController = ComparisonControllers.StopWhenDifferent;
            XmlElement e1 = doc.CreateElement("x");
            XmlElement e2 = doc.CreateElement("x");
            Assert.AreEqual(Wrap(ComparisonResult.EQUAL),
                            d.CompareNodes(e1, new XPathContext(),
                                           e2, new XPathContext()));
            e1.AppendChild(doc.CreateElement("x"));
            Assert.AreEqual(WrapAndStop(ComparisonResult.DIFFERENT),
                            d.CompareNodes(e1, new XPathContext(),
                                           e2, new XPathContext()));
            Assert.AreEqual(1, ex.invoked);
            e2.AppendChild(doc.CreateElement("x"));
            Assert.AreEqual(Wrap(ComparisonResult.EQUAL),
                            d.CompareNodes(e1, new XPathContext(),
                                           e2, new XPathContext()));
            e2.AppendChild(doc.CreateElement("x"));
            Assert.AreEqual(WrapAndStop(ComparisonResult.DIFFERENT),
                            d.CompareNodes(e1, new XPathContext(),
                                           e2, new XPathContext()));
            Assert.AreEqual(2, ex.invoked);
        }

        [Test]
        public void CompareCharacterData() {
            DOMDifferenceEngine d = new DOMDifferenceEngine();
            DiffExpecter ex = new DiffExpecter(ComparisonType.TEXT_VALUE, 9);
            d.DifferenceListener += ex.ComparisonPerformed;
            d.DifferenceEvaluator = delegate(Comparison comparison,
                                             ComparisonResult outcome) {
                if (comparison.Type == ComparisonType.NODE_TYPE) {
                    if (outcome == ComparisonResult.EQUAL
                        || (
                            comparison.ControlDetails.Target is XmlCharacterData
                            &&
                            comparison.TestDetails.Target is XmlCharacterData)) {
                        return ComparisonResult.EQUAL;
                    }
                }
                return outcome;
            };
            d.ComparisonController = ComparisonControllers.StopWhenDifferent;

            XmlComment fooComment = doc.CreateComment("foo");
            XmlComment barComment = doc.CreateComment("bar");
            XmlText fooText = doc.CreateTextNode("foo");
            XmlText barText = doc.CreateTextNode("bar");
            XmlCDataSection fooCDataSection = doc.CreateCDataSection("foo");
            XmlCDataSection barCDataSection = doc.CreateCDataSection("bar");

            Assert.AreEqual(Wrap(ComparisonResult.EQUAL),
                            d.CompareNodes(fooComment, new XPathContext(),
                                           fooComment, new XPathContext()));
            Assert.AreEqual(WrapAndStop(ComparisonResult.DIFFERENT),
                            d.CompareNodes(fooComment, new XPathContext(),
                                           barComment, new XPathContext()));
            Assert.AreEqual(Wrap(ComparisonResult.EQUAL),
                            d.CompareNodes(fooText, new XPathContext(),
                                           fooText, new XPathContext()));
            Assert.AreEqual(WrapAndStop(ComparisonResult.DIFFERENT),
                            d.CompareNodes(fooText, new XPathContext(),
                                           barText, new XPathContext()));
            Assert.AreEqual(Wrap(ComparisonResult.EQUAL),
                            d.CompareNodes(fooCDataSection, new XPathContext(),
                                           fooCDataSection, new XPathContext()));
            Assert.AreEqual(WrapAndStop(ComparisonResult.DIFFERENT),
                            d.CompareNodes(fooCDataSection, new XPathContext(),
                                           barCDataSection, new XPathContext()));

            Assert.AreEqual(Wrap(ComparisonResult.EQUAL),
                            d.CompareNodes(fooComment, new XPathContext(),
                                           fooText, new XPathContext()));
            Assert.AreEqual(WrapAndStop(ComparisonResult.DIFFERENT),
                            d.CompareNodes(fooComment, new XPathContext(),
                                           barText, new XPathContext()));
            Assert.AreEqual(Wrap(ComparisonResult.EQUAL),
                            d.CompareNodes(fooComment, new XPathContext(),
                                           fooCDataSection, new XPathContext()));
            Assert.AreEqual(WrapAndStop(ComparisonResult.DIFFERENT),
                            d.CompareNodes(fooComment, new XPathContext(),
                                           barCDataSection, new XPathContext()));
            Assert.AreEqual(Wrap(ComparisonResult.EQUAL),
                            d.CompareNodes(fooText, new XPathContext(),
                                           fooComment, new XPathContext()));
            Assert.AreEqual(WrapAndStop(ComparisonResult.DIFFERENT),
                            d.CompareNodes(fooText, new XPathContext(),
                                           barComment, new XPathContext()));
            Assert.AreEqual(Wrap(ComparisonResult.EQUAL),
                            d.CompareNodes(fooText, new XPathContext(),
                                           fooCDataSection, new XPathContext()));
            Assert.AreEqual(WrapAndStop(ComparisonResult.DIFFERENT),
                            d.CompareNodes(fooText, new XPathContext(),
                                           barCDataSection, new XPathContext()));
            Assert.AreEqual(Wrap(ComparisonResult.EQUAL),
                            d.CompareNodes(fooCDataSection, new XPathContext(),
                                           fooText, new XPathContext()));
            Assert.AreEqual(WrapAndStop(ComparisonResult.DIFFERENT),
                            d.CompareNodes(fooCDataSection, new XPathContext(),
                                           barText, new XPathContext()));
            Assert.AreEqual(Wrap(ComparisonResult.EQUAL),
                            d.CompareNodes(fooCDataSection, new XPathContext(),
                                           fooComment, new XPathContext()));
            Assert.AreEqual(WrapAndStop(ComparisonResult.DIFFERENT),
                            d.CompareNodes(fooCDataSection, new XPathContext(),
                                           barComment, new XPathContext()));
            Assert.AreEqual(9, ex.invoked);
        }

        [Test]
        public void CompareProcessingInstructions() {
            DOMDifferenceEngine d = new DOMDifferenceEngine();
            DiffExpecter ex =
                new DiffExpecter(ComparisonType.PROCESSING_INSTRUCTION_TARGET);
            d.DifferenceListener += ex.ComparisonPerformed;
            d.ComparisonController = ComparisonControllers.StopWhenDifferent;
            
            XmlProcessingInstruction foo1 = doc.CreateProcessingInstruction("foo",
                                                                            "1");
            XmlProcessingInstruction bar1 = doc.CreateProcessingInstruction("bar",
                                                                            "1");
            Assert.AreEqual(Wrap(ComparisonResult.EQUAL),
                            d.CompareNodes(foo1, new XPathContext(),
                                           foo1, new XPathContext()));
            Assert.AreEqual(WrapAndStop(ComparisonResult.DIFFERENT),
                            d.CompareNodes(foo1, new XPathContext(),
                                           bar1, new XPathContext()));
            Assert.AreEqual(1, ex.invoked);

            d = new DOMDifferenceEngine();
            ex = new DiffExpecter(ComparisonType.PROCESSING_INSTRUCTION_DATA);
            d.DifferenceListener += ex.ComparisonPerformed;
            d.ComparisonController = ComparisonControllers.StopWhenDifferent;
            XmlProcessingInstruction foo2 = doc.CreateProcessingInstruction("foo",
                                                                            "2");
            Assert.AreEqual(Wrap(ComparisonResult.EQUAL),
                            d.CompareNodes(foo1, new XPathContext(),
                                           foo1, new XPathContext()));
            Assert.AreEqual(WrapAndStop(ComparisonResult.DIFFERENT),
                            d.CompareNodes(foo1, new XPathContext(),
                                           foo2, new XPathContext()));
            Assert.AreEqual(1, ex.invoked);
        }

        [Test]
        public void CompareDocuments() {
            DOMDifferenceEngine d = new DOMDifferenceEngine();
            DiffExpecter ex =
                new DiffExpecter(ComparisonType.HAS_DOCTYPE_DECLARATION);
            d.DifferenceListener += ex.ComparisonPerformed;
            d.DifferenceEvaluator = delegate(Comparison comparison,
                                             ComparisonResult outcome) {
                if (comparison.Type == ComparisonType.CHILD_NODELIST_LENGTH) {
                    Assert.AreEqual(ComparisonResult.DIFFERENT, outcome);
                    // downgrade so we get to see the
                    // HAS_DOCTYPE_DECLARATION difference
                    return ComparisonResult.EQUAL;
                }
                if (comparison.Type == ComparisonType.HAS_DOCTYPE_DECLARATION) {
                    Assert.AreEqual(ComparisonResult.DIFFERENT, outcome);
                    return ComparisonResult.DIFFERENT;
                }
                Assert.AreEqual(ComparisonResult.EQUAL, outcome);
                return ComparisonResult.EQUAL;
            };
            d.ComparisonController = ComparisonControllers.StopWhenDifferent;
            d.NodeFilter = NodeFilters.AcceptAll;

            XmlDocument d1, d2;

            d1 = Org.XmlUnit.Util.Convert
                .ToDocument(InputBuilder.FromString("<Book/>").Build());
            d2 = new XmlDocument();
            d2.LoadXml("<!DOCTYPE Book PUBLIC "
                       + "\"XMLUNIT/TEST/PUB\" "
                       + "\"" + TestResources.BOOK_DTD
                       + "\">"
                       + "<Book/>");
            Assert.AreEqual(WrapAndStop(ComparisonResult.DIFFERENT),
                            d.CompareNodes(d1, new XPathContext(),
                                           d2, new XPathContext()));
            Assert.AreEqual(1, ex.invoked);

#if false // .NET doesn't like XML 1.1 anyway
            d = new DOMDifferenceEngine();
            ex = new DiffExpecter(ComparisonType.XML_VERSION);
            d.DifferenceListener += ex.ComparisonPerformed;
            d.ComparisonController = ComparisonControllers.StopWhenDifferent;
            
            d1 = Org.XmlUnit.Util.Convert
                .ToDocument(InputBuilder.FromString("<?xml version=\"1.0\""
                                             + " encoding=\"UTF-8\"?>"
                                             + "<Book/>").Build());
            d2 = Org.XmlUnit.Util.Convert
                .ToDocument(InputBuilder.FromString("<?xml version=\"1.1\""
                                             + " encoding=\"UTF-8\"?>"
                                             + "<Book/>").Build());
            Assert.AreEqual(WrapAndStop(ComparisonResult.DIFFERENT),
                            d.CompareNodes(d1, new XPathContext(),
                                           d2, new XPathContext()));
            Assert.AreEqual(1, ex.invoked);
#endif

            d = new DOMDifferenceEngine();
            ex = new DiffExpecter(ComparisonType.XML_STANDALONE);
            d.DifferenceListener += ex.ComparisonPerformed;
            d.ComparisonController = ComparisonControllers.StopWhenDifferent;
            
            d1 = Org.XmlUnit.Util.Convert
                .ToDocument(InputBuilder.FromString("<?xml version=\"1.0\""
                                             + " standalone=\"yes\"?>"
                                             + "<Book/>").Build());
            d2 = Org.XmlUnit.Util.Convert
                .ToDocument(InputBuilder.FromString("<?xml version=\"1.0\""
                                             + " standalone=\"no\"?>"
                                             + "<Book/>").Build());
            Assert.AreEqual(WrapAndStop(ComparisonResult.DIFFERENT),
                            d.CompareNodes(d1, new XPathContext(),
                                           d2, new XPathContext()));
            Assert.AreEqual(1, ex.invoked);

            d = new DOMDifferenceEngine();
            ex = new DiffExpecter(ComparisonType.XML_ENCODING);
            d.DifferenceListener += ex.ComparisonPerformed;
            d.DifferenceEvaluator = delegate(Comparison comparison,
                                             ComparisonResult outcome) {
                if (comparison.Type == ComparisonType.XML_ENCODING) {
                    Assert.AreEqual(ComparisonResult.DIFFERENT, outcome);
                    return ComparisonResult.DIFFERENT;
                }
                Assert.AreEqual(ComparisonResult.EQUAL, outcome);
                return ComparisonResult.EQUAL;
            };
            d.ComparisonController = ComparisonControllers.StopWhenDifferent;

            d1 = Org.XmlUnit.Util.Convert
                .ToDocument(InputBuilder.FromString("<?xml version=\"1.0\""
                                             + " encoding=\"UTF-8\"?>"
                                             + "<Book/>").Build());
            d2 = Org.XmlUnit.Util.Convert
                .ToDocument(InputBuilder.FromString("<?xml version=\"1.0\""
                                             + " encoding=\"UTF-16\"?>"
                                             + "<Book/>").Build());
            Assert.AreEqual(WrapAndStop(ComparisonResult.DIFFERENT),
                            d.CompareNodes(d1, new XPathContext(),
                                           d2, new XPathContext()));
            Assert.AreEqual(1, ex.invoked);
        }

        [Test]
        public void NodeFilterAppliesToDocTypes() {
            DOMDifferenceEngine d = new DOMDifferenceEngine();
            DiffExpecter ex =
                new DiffExpecter(ComparisonType.HAS_DOCTYPE_DECLARATION);
            d.DifferenceListener += ex.ComparisonPerformed;
            d.ComparisonController = ComparisonControllers.StopWhenDifferent;

            XmlDocument d1, d2;

            d1 = Org.XmlUnit.Util.Convert
                .ToDocument(InputBuilder.FromString("<Book/>").Build());
            d2 = new XmlDocument();
            d2.LoadXml("<!DOCTYPE Book PUBLIC "
                       + "\"XMLUNIT/TEST/PUB\" "
                       + "\"" + TestResources.BOOK_DTD
                       + "\">"
                       + "<Book/>");
            Assert.AreEqual(Wrap(ComparisonResult.EQUAL),
                            d.CompareNodes(d1, new XPathContext(),
                                           d2, new XPathContext()));
            Assert.AreEqual(0, ex.invoked);
        }

        [Test]
        public void CompareDocTypes() {
            DOMDifferenceEngine d = new DOMDifferenceEngine();
            DiffExpecter ex = new DiffExpecter(ComparisonType.DOCTYPE_NAME);
            d.DifferenceListener += ex.ComparisonPerformed;
            d.ComparisonController = ComparisonControllers.StopWhenDifferent;
            
            XmlDocumentType dt1 = doc.CreateDocumentType("name", "pub",
                                                         TestResources.BOOK_DTD,
                                                         null);
            XmlDocumentType dt2 = doc.CreateDocumentType("name2", "pub",
                                                         TestResources.BOOK_DTD,
                                                         null);
            Assert.AreEqual(WrapAndStop(ComparisonResult.DIFFERENT),
                            d.CompareNodes(dt1, new XPathContext(),
                                           dt2, new XPathContext()));
            Assert.AreEqual(1, ex.invoked);

            d = new DOMDifferenceEngine();
            ex = new DiffExpecter(ComparisonType.DOCTYPE_PUBLIC_ID);
            d.DifferenceListener += ex.ComparisonPerformed;
            d.ComparisonController = ComparisonControllers.StopWhenDifferent;
            dt2 = doc.CreateDocumentType("name", "pub2",
                                         TestResources.BOOK_DTD, null);
            Assert.AreEqual(WrapAndStop(ComparisonResult.DIFFERENT),
                            d.CompareNodes(dt1, new XPathContext(),
                                           dt2, new XPathContext()));
            Assert.AreEqual(1, ex.invoked);

            d = new DOMDifferenceEngine();
            ex = new DiffExpecter(ComparisonType.DOCTYPE_SYSTEM_ID);
            d.DifferenceListener += ex.ComparisonPerformed;
            d.DifferenceEvaluator = delegate(Comparison comparison,
                                             ComparisonResult outcome) {
                if (comparison.Type == ComparisonType.DOCTYPE_SYSTEM_ID) {
                    Assert.AreEqual(ComparisonResult.DIFFERENT, outcome);
                    return ComparisonResult.DIFFERENT;
                }
                Assert.AreEqual(ComparisonResult.EQUAL, outcome);
                return ComparisonResult.EQUAL;
            };
            d.ComparisonController = ComparisonControllers.StopWhenDifferent;
            dt2 = doc.CreateDocumentType("name", "pub",
                                         TestResources.TEST_DTD, null);
            Assert.AreEqual(WrapAndStop(ComparisonResult.DIFFERENT),
                            d.CompareNodes(dt1, new XPathContext(),
                                           dt2, new XPathContext()));
            Assert.AreEqual(1, ex.invoked);
        }

        [Test]
        public void CompareElements() {
            DOMDifferenceEngine d = new DOMDifferenceEngine();
            DiffExpecter ex = new DiffExpecter(ComparisonType.ELEMENT_TAG_NAME);
            d.DifferenceListener += ex.ComparisonPerformed;
            d.ComparisonController = ComparisonControllers.StopWhenDifferent;
            
            XmlElement e1 = doc.CreateElement("foo");
            XmlElement e2 = doc.CreateElement("foo");
            XmlElement e3 = doc.CreateElement("bar");
            Assert.AreEqual(Wrap(ComparisonResult.EQUAL),
                            d.CompareNodes(e1, new XPathContext(),
                                           e2, new XPathContext()));
            Assert.AreEqual(WrapAndStop(ComparisonResult.DIFFERENT),
                            d.CompareNodes(e1, new XPathContext(),
                                           e3, new XPathContext()));
            Assert.AreEqual(1, ex.invoked);

            d = new DOMDifferenceEngine();
            ex = new DiffExpecter(ComparisonType.ELEMENT_NUM_ATTRIBUTES);
            d.DifferenceListener += ex.ComparisonPerformed;
            d.ComparisonController = ComparisonControllers.StopWhenDifferent;
            e1.SetAttribute("attr1", "value1");
            Assert.AreEqual(WrapAndStop(ComparisonResult.DIFFERENT),
                            d.CompareNodes(e1, new XPathContext(),
                                           e2, new XPathContext()));
            Assert.AreEqual(1, ex.invoked);

            d = new DOMDifferenceEngine();
            ex = new DiffExpecter(ComparisonType.ATTR_NAME_LOOKUP,
                                  "/@attr1", "/");
            d.DifferenceListener += ex.ComparisonPerformed;
            d.ComparisonController = ComparisonControllers.StopWhenDifferent;
            e2.SetAttribute("attr1", "urn:xmlunit:test", "value1");
            Assert.AreEqual(WrapAndStop(ComparisonResult.DIFFERENT),
                            d.CompareNodes(e1, new XPathContext(),
                                           e2, new XPathContext()));
            Assert.AreEqual(1, ex.invoked);

            d = new DOMDifferenceEngine();
            d.DifferenceListener += delegate(Comparison comp,
                                             ComparisonResult r) {
                Assert.Fail("unexpected Comparison of type " + comp.Type
                            + " with outcome " + r + " and values '"
                            + comp.ControlDetails.Value
                            + "' and '"
                            + comp.TestDetails.Value + "'"
                            + " on '" + comp.ControlDetails.Target + "'");
            };
            d.ComparisonController = ComparisonControllers.StopWhenDifferent;
            e1.SetAttribute("attr1", "urn:xmlunit:test", "value1");
            e2.SetAttribute("attr1", null, "value1");
            Assert.AreEqual(Wrap(ComparisonResult.EQUAL),
                            d.CompareNodes(e1, new XPathContext(),
                                           e2, new XPathContext()));
        }

        [Test]
        public void CompareAttributes() {
            XmlAttribute a1 = doc.CreateAttribute("foo");
            XmlAttribute a2 = doc.CreateAttribute("foo");

            DOMDifferenceEngine d = new DOMDifferenceEngine();
#if false // Can't reset "explicitly set" state for Documents created via API
            DiffExpecter ex = new DiffExpecter(ComparisonType.ATTR_VALUE_EXPLICITLY_SPECIFIED);
            d.DifferenceListener += ex.ComparisonPerformed;
            d.DifferenceEvaluator = DifferenceEvaluators.Accept;
            a2.Value = string.Empty;
            Assert.AreEqual(WrapAndStop(ComparisonResult.DIFFERENT),
                            d.CompareNodes(a1, new XPathContext(),
                                           a2, new XPathContext()));
            Assert.AreEqual(1, ex.invoked);
#endif

            d = new DOMDifferenceEngine();
            DiffExpecter ex = new DiffExpecter(ComparisonType.ATTR_VALUE);
            d.DifferenceListener += ex.ComparisonPerformed;
            d.ComparisonController = ComparisonControllers.StopWhenDifferent;
            XmlAttribute a3 = doc.CreateAttribute("foo");
            a1.Value = "foo";
            a2.Value = "foo";
            a3.Value = "bar";
            Assert.AreEqual(Wrap(ComparisonResult.EQUAL),
                            d.CompareNodes(a1, new XPathContext(),
                                           a2, new XPathContext()));
            Assert.AreEqual(WrapAndStop(ComparisonResult.DIFFERENT),
                            d.CompareNodes(a1, new XPathContext(),
                                           a3, new XPathContext()));
            Assert.AreEqual(1, ex.invoked);
        }

        [Test]
        public void CompareAttributesWithAttributeFilter() {
            DOMDifferenceEngine d = new DOMDifferenceEngine();
            d.AttributeFilter = a => "x" == a.Name;
            DiffExpecter ex = new DiffExpecter(ComparisonType.ATTR_VALUE);
            d.DifferenceListener += ex.ComparisonPerformed;
            d.ComparisonController = ComparisonControllers.StopWhenDifferent;

            XmlElement e1 = doc.CreateElement("foo");
            e1.SetAttribute("x", "1");
            e1.SetAttribute("a", "xxx");
            XmlElement e2 = doc.CreateElement("foo");
            e2.SetAttribute("x", "1");
            e2.SetAttribute("b", "xxx");
            e2.SetAttribute("c", "xxx");
            XmlElement e3 = doc.CreateElement("foo");
            e3.SetAttribute("x", "3");

            Assert.AreEqual(Wrap(ComparisonResult.EQUAL),
                            d.CompareNodes(e1, new XPathContext(),
                                           e2, new XPathContext()));
            Assert.AreEqual(WrapAndStop(ComparisonResult.DIFFERENT),
                            d.CompareNodes(e1, new XPathContext(),
                                           e3, new XPathContext()));
            Assert.AreEqual(1, ex.invoked);
        }

        [Test]
        public void CompareNodesWithNodeFilter() {
            DOMDifferenceEngine d = new DOMDifferenceEngine();
            d.NodeFilter = n => "x" == n.Name || "foo" == n.Name;
            DiffExpecter ex = new DiffExpecter(ComparisonType.CHILD_NODELIST_LENGTH,
                                               "/", "/");
            d.DifferenceListener += ex.ComparisonPerformed;
            d.ComparisonController = ComparisonControllers.StopWhenDifferent;

            XmlElement e1 = doc.CreateElement("foo");
            e1.AppendChild(doc.CreateElement("x"));
            e1.AppendChild(doc.CreateElement("y"));
            XmlElement e2 = doc.CreateElement("foo");
            e2.AppendChild(doc.CreateElement("x"));
            e2.AppendChild(doc.CreateElement("y"));
            e2.AppendChild(doc.CreateElement("z"));
            XmlElement e3 = doc.CreateElement("foo");
            e3.AppendChild(doc.CreateElement("y"));

            Assert.AreEqual(Wrap(ComparisonResult.EQUAL),
                            d.CompareNodes(e1, new XPathContext(),
                                           e2, new XPathContext()));
            Assert.AreEqual(WrapAndStop(ComparisonResult.DIFFERENT),
                            d.CompareNodes(e1, new XPathContext(),
                                           e3, new XPathContext()));
            Assert.AreEqual(1, ex.invoked);
        }

        [Test]
        public void NaiveRecursion() {
            XmlElement e1 = doc.CreateElement("foo");
            XmlElement e2 = doc.CreateElement("foo");
            XmlElement c1 = doc.CreateElement("bar");
            e1.AppendChild(c1);
            DOMDifferenceEngine d = new DOMDifferenceEngine();
            DiffExpecter ex = new DiffExpecter(ComparisonType.CHILD_LOOKUP,
                                               "/bar[1]", null).WithParentXPath("/", "/");
            d.DifferenceListener += ex.ComparisonPerformed;
            DifferenceEvaluator ev = delegate(Comparison comparison,
                                              ComparisonResult outcome) {
                if (comparison.Type == ComparisonType.CHILD_NODELIST_LENGTH) {
                    return ComparisonResult.EQUAL;
                }
                return outcome;
            };
            d.ComparisonController = ComparisonControllers.StopWhenDifferent;
            d.DifferenceEvaluator = ev;
            Assert.AreEqual(WrapAndStop(ComparisonResult.DIFFERENT),
                            d.CompareNodes(e1, new XPathContext(),
                                           e2, new XPathContext()));
            Assert.AreEqual(1, ex.invoked);

            // symmetric?
            d = new DOMDifferenceEngine();
            ex = new DiffExpecter(ComparisonType.CHILD_LOOKUP,
                                  null, "/bar[1]").WithParentXPath("/", "/");
            d.DifferenceListener += ex.ComparisonPerformed;
            d.DifferenceEvaluator = ev;
            d.ComparisonController = ComparisonControllers.StopWhenDifferent;
            Assert.AreEqual(WrapAndStop(ComparisonResult.DIFFERENT),
                            d.CompareNodes(e2, new XPathContext(),
                                           e1, new XPathContext()));
            Assert.AreEqual(1, ex.invoked);

            XmlElement c2 = doc.CreateElement("bar");
            e2.AppendChild(c2);
            d = new DOMDifferenceEngine();
            ex = new DiffExpecter(ComparisonType.CHILD_LOOKUP);
            d.DifferenceListener += ex.ComparisonPerformed;
            d.DifferenceEvaluator = ev;
            d.ComparisonController = ComparisonControllers.StopWhenDifferent;
            Assert.AreEqual(Wrap(ComparisonResult.EQUAL),
                            d.CompareNodes(e1, new XPathContext(),
                                           e2, new XPathContext()));
            Assert.AreEqual(Wrap(ComparisonResult.EQUAL),
                            d.CompareNodes(e2, new XPathContext(),
                                           e1, new XPathContext()));
            Assert.AreEqual(0, ex.invoked);
        }

        [Test]
        // see https://sourceforge.net/p/xmlunit/discussion/73273/thread/92c980ec5b/
        public void SourceforgeForumThread92c980ec5b() {
            XmlElement gp1 = doc.CreateElement("grandparent");
            XmlElement p1_0 = doc.CreateElement("parent");
            p1_0.SetAttribute("id", "0");
            gp1.AppendChild(p1_0);
            XmlElement p1_1 = doc.CreateElement("parent");
            p1_1.SetAttribute("id", "1");
            gp1.AppendChild(p1_1);
            XmlElement c1_1 = doc.CreateElement("child");
            c1_1.SetAttribute("id", "1");
            p1_1.AppendChild(c1_1);

            XmlElement gp2 = doc.CreateElement("grandparent");
            XmlElement p2_1 = doc.CreateElement("parent");
            p2_1.SetAttribute("id", "1");
            gp2.AppendChild(p2_1);
            XmlElement c2_1 = doc.CreateElement("child");
            c2_1.SetAttribute("id", "1");
            p2_1.AppendChild(c2_1);
            XmlElement c2_2 = doc.CreateElement("child");
            c2_2.SetAttribute("id", "2");
            p2_1.AppendChild(c2_2);

            DOMDifferenceEngine d = new DOMDifferenceEngine();
            DiffExpecter ex = new DiffExpecter(ComparisonType.CHILD_LOOKUP,
                                               null, "/grandparent[1]/parent[1]/child[2]")
                .WithParentXPath("/grandparent[1]/parent[2]", "/grandparent[1]/parent[1]");
            d.DifferenceListener += ex.ComparisonPerformed;
            DifferenceEvaluator ev = delegate(Comparison comparison,
                                              ComparisonResult outcome) {
                if (comparison.Type == ComparisonType.CHILD_NODELIST_LENGTH
                    || comparison.Type == ComparisonType.CHILD_NODELIST_SEQUENCE) {
                    return ComparisonResult.EQUAL;
                }
                if (comparison.Type == ComparisonType.CHILD_LOOKUP
                    && comparison.TestDetails.Target == null) {
                    return ComparisonResult.EQUAL;
                }
                return outcome;
            };
            d.ComparisonController = ComparisonControllers.StopWhenDifferent;
            d.DifferenceEvaluator = ev;
            d.NodeMatcher = new DefaultNodeMatcher(ElementSelectors.ByNameAndAllAttributes);
            d.NodeFilter = n =>
                n.NodeType != XmlNodeType.DocumentType &&
                    !("parent" == n.Name && "0" == n.Attributes["id"].Value);
            Assert.AreEqual(WrapAndStop(ComparisonResult.DIFFERENT),
                            d.CompareNodes(gp1, new XPathContext(gp1),
                                           gp2, new XPathContext(gp2)));
            Assert.AreEqual(1, ex.invoked);
        }

        [Test] 
        public void TextAndCDataMatchRecursively() {
            XmlElement e1 = doc.CreateElement("foo");
            XmlElement e2 = doc.CreateElement("foo");
            XmlText fooText = doc.CreateTextNode("foo");
            e1.AppendChild(fooText);
            XmlCDataSection fooCDATASection = doc.CreateCDataSection("foo");
            e2.AppendChild(fooCDATASection);
            DOMDifferenceEngine d = new DOMDifferenceEngine();
            Assert.AreEqual(Wrap(ComparisonResult.EQUAL),
                            d.CompareNodes(e1, new XPathContext(),
                                           e2, new XPathContext()));
            Assert.AreEqual(Wrap(ComparisonResult.EQUAL),
                            d.CompareNodes(e2, new XPathContext(),
                                           e1, new XPathContext()));
        }

        [Test]
        public void RecursionUsesElementSelector() {
            XmlElement e1 = doc.CreateElement("foo");
            XmlElement e2 = doc.CreateElement("foo");
            XmlElement e3 = doc.CreateElement("bar");
            e1.AppendChild(e3);
            XmlElement e4 = doc.CreateElement("baz");
            e2.AppendChild(e4);
            DOMDifferenceEngine d = new DOMDifferenceEngine();
            DiffExpecter ex = new DiffExpecter(ComparisonType.ELEMENT_TAG_NAME,
                                               "/bar[1]", "/baz[1]");
            d.DifferenceListener += ex.ComparisonPerformed;
            d.ComparisonController = ComparisonControllers.StopWhenDifferent;
            Assert.AreEqual(WrapAndStop(ComparisonResult.DIFFERENT),
                            d.CompareNodes(e1, new XPathContext(),
                                           e2, new XPathContext()));
            Assert.AreEqual(1, ex.invoked);

            d = new DOMDifferenceEngine();
            d.NodeMatcher = new DefaultNodeMatcher(ElementSelectors.ByName);
            ex = new DiffExpecter(ComparisonType.CHILD_LOOKUP);
            d.DifferenceListener += ex.ComparisonPerformed;
            d.ComparisonController = ComparisonControllers.StopWhenDifferent;
            Assert.AreEqual(WrapAndStop(ComparisonResult.DIFFERENT),
                            d.CompareNodes(e1, new XPathContext(),
                                           e2, new XPathContext()));
            Assert.AreEqual(1, ex.invoked);
        }

        [Test]
        public void SchemaLocationDifferences() {
            XmlElement e1 = doc.CreateElement("foo");
            XmlElement e2 = doc.CreateElement("foo");
            e1.SetAttribute("schemaLocation",
                            "http://www.w3.org/2001/XMLSchema-instance",
                            "somewhere");
            e2.SetAttribute("schemaLocation",
                            "http://www.w3.org/2001/XMLSchema-instance",
                            "somewhere else");

            DOMDifferenceEngine d = new DOMDifferenceEngine();
            DiffExpecter ex = new DiffExpecter(ComparisonType.SCHEMA_LOCATION);
            d.DifferenceListener += ex.ComparisonPerformed;
            d.DifferenceEvaluator = delegate(Comparison comparison,
                                             ComparisonResult outcome) {
                if (comparison.Type == ComparisonType.SCHEMA_LOCATION) {
                    Assert.AreEqual(ComparisonResult.DIFFERENT, outcome);
                    return ComparisonResult.DIFFERENT;
                }
                Assert.AreEqual(ComparisonResult.EQUAL, outcome);
                return ComparisonResult.EQUAL;
            };
            d.ComparisonController = ComparisonControllers.StopWhenDifferent;
            Assert.AreEqual(WrapAndStop(ComparisonResult.DIFFERENT),
                            d.CompareNodes(e1, new XPathContext(),
                                           e2, new XPathContext()));
            Assert.AreEqual(1, ex.invoked);

            e1 = doc.CreateElement("foo");
            e2 = doc.CreateElement("foo");
            e1.SetAttribute("noNamespaceSchemaLocation",
                            "http://www.w3.org/2001/XMLSchema-instance",
                            "somewhere");
            e2.SetAttribute("noNamespaceSchemaLocation",
                            "http://www.w3.org/2001/XMLSchema-instance",
                            "somewhere else");
            d = new DOMDifferenceEngine();
            ex = new DiffExpecter(ComparisonType.NO_NAMESPACE_SCHEMA_LOCATION);
            d.DifferenceListener += ex.ComparisonPerformed;
            d.DifferenceEvaluator = delegate(Comparison comparison,
                                             ComparisonResult outcome) {
                if (comparison.Type == ComparisonType.NO_NAMESPACE_SCHEMA_LOCATION) {
                    Assert.AreEqual(ComparisonResult.DIFFERENT, outcome);
                    return ComparisonResult.DIFFERENT;
                }
                Assert.AreEqual(ComparisonResult.EQUAL, outcome);
                return ComparisonResult.EQUAL;
            };
            d.ComparisonController = ComparisonControllers.StopWhenDifferent;
            Assert.AreEqual(WrapAndStop(ComparisonResult.DIFFERENT),
                            d.CompareNodes(e1, new XPathContext(),
                                           e2, new XPathContext()));
            Assert.AreEqual(1, ex.invoked);
        }

        [Test]
        public void CompareElementsNS() {
            DOMDifferenceEngine d = new DOMDifferenceEngine();
            DiffExpecter ex = new DiffExpecter(ComparisonType.ELEMENT_TAG_NAME);
            d.DifferenceListener += ex.ComparisonPerformed;
            DifferenceEvaluator ev = delegate(Comparison comparison,
                                              ComparisonResult outcome) {
                if (comparison.Type == ComparisonType.NAMESPACE_PREFIX) {
                    return ComparisonResult.EQUAL;
                }
                return outcome;
            };
            d.ComparisonController = ComparisonControllers.StopWhenDifferent;
            d.DifferenceEvaluator = ev;

            XmlElement e1 = doc.CreateElement("p1", "foo", "urn:xmlunit:test");
            XmlElement e2 = doc.CreateElement("p1", "foo", "urn:xmlunit:test");
            Assert.AreEqual(Wrap(ComparisonResult.EQUAL),
                            d.CompareNodes(e1, new XPathContext(),
                                           e2, new XPathContext()));
            Assert.AreEqual(0, ex.invoked);
        }

        [Test]
        public void ChildNodeListSequence() {
            XmlElement e1 = doc.CreateElement("foo");
            XmlElement e3 = doc.CreateElement("bar");
            XmlElement e4 = doc.CreateElement("baz");
            e1.AppendChild(e3);
            e1.AppendChild(e4);

            XmlElement e2 = doc.CreateElement("foo");
            XmlElement e5 = doc.CreateElement("bar");
            XmlElement e6 = doc.CreateElement("baz");
            e2.AppendChild(e6);
            e2.AppendChild(e5);

            DOMDifferenceEngine d = new DOMDifferenceEngine();
            DiffExpecter ex = new DiffExpecter(ComparisonType.CHILD_NODELIST_SEQUENCE,
                                               "/bar[1]", "/bar[1]");
            d.DifferenceListener += ex.ComparisonPerformed;
            DifferenceEvaluator ev = delegate(Comparison comparison,
                                              ComparisonResult outcome) {
                if (outcome != ComparisonResult.EQUAL
                    && comparison.Type == ComparisonType.CHILD_NODELIST_SEQUENCE) {
                    return ComparisonResult.DIFFERENT;
                }
                return outcome;
            };
            d.DifferenceEvaluator = ev;
            d.ComparisonController = ComparisonControllers.StopWhenDifferent;
            d.NodeMatcher = new DefaultNodeMatcher(ElementSelectors.ByName);

            Assert.AreEqual(WrapAndStop(ComparisonResult.DIFFERENT),
                            d.CompareNodes(e1, new XPathContext(),
                                           e2, new XPathContext()));
            Assert.AreEqual(1, ex.invoked);
        }

        [Test]
        public void XsiTypesWithDifferentPrefixes() {
            XmlDocument d1 =
                DocumentForString("<foo xsi:type='p1:Foo'"
                                  + " xmlns:p1='urn:xmlunit:test'"
                                  + " xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'"
                                  + "/>");
            XmlDocument d2 =
                DocumentForString("<foo xsi:type='p2:Foo'"
                                  + " xmlns:p2='urn:xmlunit:test'"
                                  + " xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'"
                                  + "/>");
            DOMDifferenceEngine d = new DOMDifferenceEngine();
            DiffExpecter ex = new DiffExpecter(ComparisonType.ATTR_VALUE);
            d.DifferenceListener += ex.ComparisonPerformed;
            d.ComparisonController = ComparisonControllers.StopWhenDifferent;
            Assert.AreEqual(Wrap(ComparisonResult.EQUAL),
                            d.CompareNodes(d1, new XPathContext(),
                                           d2, new XPathContext()));
        }

        [Test]
        public void XsiTypesWithDefaultNamespace() {
            XmlDocument d1 =
                DocumentForString("<a:foo xsi:type='Foo'"
                                  + " xmlns='urn:xmlunit:test'"
                                  + " xmlns:a='urn:xmlunit:test2'"
                                  + " xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'"
                                  + "/>");
            XmlDocument d2 =
                DocumentForString("<a:foo xsi:type='p2:Foo'"
                                  + " xmlns:p2='urn:xmlunit:test'"
                                  + " xmlns:a='urn:xmlunit:test2'"
                                  + " xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'"
                                  + "/>");
            DOMDifferenceEngine d = new DOMDifferenceEngine();
            DiffExpecter ex = new DiffExpecter(ComparisonType.ATTR_VALUE);
            d.DifferenceListener += ex.ComparisonPerformed;
            d.ComparisonController = ComparisonControllers.StopWhenDifferent;
            Assert.AreEqual(Wrap(ComparisonResult.EQUAL),
                            d.CompareNodes(d1, new XPathContext(),
                                           d2, new XPathContext()));
        }

        [Test]
        public void XsiTypesWithDifferentLocalNames() {
            XmlDocument d1 =
                DocumentForString("<foo xsi:type='p1:Bar'"
                                  + " xmlns:p1='urn:xmlunit:test'"
                                  + " xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'"
                                  + "/>");
            XmlDocument d2 =
                DocumentForString("<foo xsi:type='p1:Foo'"
                                  + " xmlns:p1='urn:xmlunit:test'"
                                  + " xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'"
                                  + "/>");
            DOMDifferenceEngine d = new DOMDifferenceEngine();
            DiffExpecter ex = new DiffExpecter(ComparisonType.ATTR_VALUE);
            d.DifferenceListener += ex.ComparisonPerformed;
            d.ComparisonController = ComparisonControllers.StopWhenDifferent;
            Assert.AreEqual(WrapAndStop(ComparisonResult.DIFFERENT),
                            d.CompareNodes(d1, new XPathContext(),
                                           d2, new XPathContext()));
        }

        [Test]
        public void XsiTypesWithDifferentNamespaceURIs() {
            XmlDocument d1 =
                DocumentForString("<foo xsi:type='p1:Foo'"
                                  + " xmlns:p1='urn:xmlunit:test'"
                                  + " xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'"
                                  + "/>");
            XmlDocument d2 =
                DocumentForString("<foo xsi:type='p1:Foo'"
                                  + " xmlns:p1='urn:xmlunit:test2'"
                                  + " xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'"
                                  + "/>");
            DOMDifferenceEngine d = new DOMDifferenceEngine();
            DiffExpecter ex = new DiffExpecter(ComparisonType.ATTR_VALUE);
            d.DifferenceListener += ex.ComparisonPerformed;
            d.ComparisonController = ComparisonControllers.StopWhenDifferent;
            Assert.AreEqual(WrapAndStop(ComparisonResult.DIFFERENT),
                            d.CompareNodes(d1, new XPathContext(),
                                           d2, new XPathContext()));
        }

        [Test]
        public void XsiTypesWithNamespaceDeclarationOnDifferentLevels() {
            XmlDocument d1 =
                DocumentForString("<bar xmlns:p1='urn:xmlunit:test'>"
                                  + "<foo xsi:type='p1:Foo'"
                                  + " xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'"
                                  + "/></bar>");
            XmlDocument d2 =
                DocumentForString("<bar><foo xsi:type='p1:Foo'"
                                  + " xmlns:p1='urn:xmlunit:test'"
                                  + " xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'"
                                  + "/></bar>");
            DOMDifferenceEngine d = new DOMDifferenceEngine();
            DiffExpecter ex = new DiffExpecter(ComparisonType.ATTR_VALUE);
            d.DifferenceListener += ex.ComparisonPerformed;
            d.ComparisonController = ComparisonControllers.StopWhenDifferent;
            Assert.AreEqual(Wrap(ComparisonResult.EQUAL),
                            d.CompareNodes(d1, new XPathContext(),
                                           d2, new XPathContext()));
        }

        [Test]
        public void XsiNil() {
            XmlDocument d1 =
                DocumentForString("<foo xsi:nil='true'"
                                  + " xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'"
                                  + "/>");
            XmlDocument d2 =
                DocumentForString("<foo xsi:nil='false'"
                                  + " xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'"
                                  + "/>");
            DOMDifferenceEngine d = new DOMDifferenceEngine();
            DiffExpecter ex = new DiffExpecter(ComparisonType.ATTR_VALUE);
            d.DifferenceListener += ex.ComparisonPerformed;
            d.ComparisonController = ComparisonControllers.StopWhenDifferent;
            Assert.AreEqual(WrapAndStop(ComparisonResult.DIFFERENT),
                            d.CompareNodes(d1, new XPathContext(),
                                           d2, new XPathContext()));
        }

        [Test]
        public void ShouldDetectCommentInPrelude() {
            DOMDifferenceEngine d = new DOMDifferenceEngine();
            XmlDocument d1 =
                Org.XmlUnit.Util.Convert.ToDocument(InputBuilder.FromFile(TestResources.TESTS_DIR
                                                  + "BookXsdGenerated.xml")
                                         .Build());
            XmlDocument d2 =
                Org.XmlUnit.Util.Convert.ToDocument(InputBuilder.FromFile(TestResources.TESTS_DIR
                                                  + "BookXsdGeneratedWithComment.xml")
                                   .Build());
            DiffExpecter ex = new DiffExpecter(ComparisonType.CHILD_NODELIST_LENGTH,
                                               "/", "/");
            d.DifferenceListener += ex.ComparisonPerformed;
            d.ComparisonController = ComparisonControllers.StopWhenDifferent;
            Assert.AreEqual(WrapAndStop(ComparisonResult.DIFFERENT),
                            d.CompareNodes(d1, new XPathContext(),
                                           d2, new XPathContext()));
        }

        [Test]
        public void ShouldDetectMissingXsiType() {
            DOMDifferenceEngine d = new DOMDifferenceEngine();
            XmlDocument d1 =
                Org.XmlUnit.Util.Convert.ToDocument(InputBuilder.FromString("<doc xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">"
                                                                            + "<effectiveTime xsi:type=\"IVL_TS\"></effectiveTime></doc>")
                                                    .Build());
            XmlDocument d2 =
                Org.XmlUnit.Util.Convert.ToDocument(InputBuilder.FromString("<doc xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">"
                                                                     + "<effectiveTime></effectiveTime></doc>")
                                                    .Build());

            DiffExpecter ex = new DiffExpecter(ComparisonType.ATTR_NAME_LOOKUP,
                                               "/doc[1]/effectiveTime[1]/@type",
                                               "/doc[1]/effectiveTime[1]");
            d.DifferenceListener += ex.ComparisonPerformed;
            d.ComparisonController = ComparisonControllers.StopWhenDifferent;
            Assert.AreEqual(WrapAndStop(ComparisonResult.DIFFERENT),
                            d.CompareNodes(d1, new XPathContext(),
                                           d2, new XPathContext()));
        }

        // https://github.com/xmlunit/xmlunit.net/issues/22
        [Test]
        public void ElementsWithDifferentPrefixesAreSimilar() {
            var diff = DiffBuilder.Compare("<Root xmlns:x='http://example.org'><x:Elem/></Root>")
                .WithTest("<Root xmlns:y='http://example.org'><y:Elem/></Root>")
                .Build();
            Assert.AreEqual(1, diff.Differences.Count());
            Assert.AreEqual(ComparisonResult.SIMILAR, diff.Differences.First().Result);
            Assert.AreEqual(ComparisonType.NAMESPACE_PREFIX, diff.Differences.First().Comparison.Type);
        }

        [Test]
        public void AttributesWithDifferentPrefixesAreSimilar() {
            var diff = DiffBuilder.Compare("<Root xmlns:x='http://example.org' x:Attr='1'/>")
                .WithTest("<Root xmlns:y='http://example.org' y:Attr='1'/>")
                .Build();
            Assert.AreEqual(1, diff.Differences.Count());
            Assert.AreEqual(ComparisonResult.SIMILAR, diff.Differences.First().Result);
            Assert.AreEqual(ComparisonType.NAMESPACE_PREFIX, diff.Differences.First().Comparison.Type);
        }

        private XmlDocument DocumentForString(string s) {
            return Org.XmlUnit.Util.Convert.ToDocument(InputBuilder.FromString(s).Build());
        }
    }
}
