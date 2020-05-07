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
using Org.XmlUnit.Diff;

namespace Org.XmlUnit.Placeholder {
    /// <summary>
    /// Handler for the "ignore" placeholder keyword.
    /// </summary>
    /// <remarks>
    ///   <para>
    /// This class and the whole module are considered experimental
    /// and any API may change between releases of XMLUnit.
    ///   </para>
    ///   <para>
    /// since 2.6.0
    ///   </para>
    /// </remarks>
    public class IgnorePlaceholderHandler : IPlaceholderHandler {
        private const string PLACEHOLDER_NAME_IGNORE = "ignore";

        /// <inheritdoc/>
        public string Keyword { get { return PLACEHOLDER_NAME_IGNORE; } }
        /// <inheritdoc/>
        public ComparisonResult Evaluate(string testText, params string[] args) {
            return ComparisonResult.EQUAL;
        }
    }
}
