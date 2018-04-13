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
using System.Text;
using System.Xml.Schema;
using NUnit.Framework.Constraints;
using Org.XmlUnit.Validation;
using InputBuilder = Org.XmlUnit.Builder.Input;

namespace Org.XmlUnit.Constraints {

    /// <summary>
    /// Constraint that validates a document against a given W3C XML
    /// schema.
    /// </summary>
    public class SchemaValidConstraint : Constraint {
        private readonly Validator validator;
        private ValidationResult result;

        /// <summary>
        /// Creates the constraint validating against the given schema(s).
        /// </summary>
        public SchemaValidConstraint(params object[] schema) : base(schema) {
            if (schema == null) {
                throw new ArgumentNullException("schema");
            }
            if (schema.Any(s => s == null)) {
                throw new ArgumentException("must not contain null values", "schema");
            }
            validator = Validator.ForLanguage(Languages.W3C_XML_SCHEMA_NS_URI);
            validator.SchemaSources = schema
                .Select(s => InputBuilder.From(s).Build())
                .ToArray();
        }

        /// <summary>
        /// Creates the constraint validating against the given schema.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///  since XMLUnit 2.3.0
        ///   </para>
        /// </remarks>
        public SchemaValidConstraint(XmlSchema schema) : base(schema) {
            if (schema == null) {
                throw new ArgumentNullException("schema");
            }
            validator = Validator.ForLanguage(Languages.W3C_XML_SCHEMA_NS_URI);
            validator.Schema = schema;
        }

        /// <inheritdoc/>
        public override bool Matches(object o) {
            this.actual = InputBuilder.From(o).Build();
            result = validator.ValidateInstance(o as ISource);
            return result.Valid;
        }

        /// <inheritdoc/>
        public override void WriteDescriptionTo(MessageWriter writer)
        {
            if (validator.Schema != null) {
                writer.Write("{0} validates against {1}",
                             GrabSystemId(actual as ISource) ?? "instance",
                             validator.Schema.SourceUri ?? " the given schema");
            } else if (validator.SchemaSources.Count(s => !string.IsNullOrEmpty(s.SystemId)) > 0) {
                writer.Write("{0} validates against {1}",
                             GrabSystemId(actual as ISource) ?? "instance",
                             GrabSystemIds());
            } else {
                writer.Write("{0} validates", GrabSystemId(actual as ISource) ?? "instance");
            }
        }

        /// <inheritdoc/>
        public override void WriteActualValueTo(MessageWriter writer)
        {
            writer.Write("got validation errors: {0}", GrabProblems());
        }

        private string GrabSystemIds() {
            return validator.SchemaSources.Select<ISource, string>(GrabSystemId)
                .Where(s => !string.IsNullOrEmpty(s))
                .Aggregate(new StringBuilder(),
                           (sb, systemId) => sb.AppendLine(systemId),
                           sb => sb.Length > 0 ? sb.Remove(sb.Length - 1, 1).ToString()
                                               : sb.ToString());
        }

        private string GrabSystemId(ISource s) {
            return s != null ? s.SystemId : null;
        }

        private string GrabProblems() {
            return result.Problems
                .Aggregate(new StringBuilder(),
                           (sb, p) => sb.AppendFormat("{0}, ", p),
                           sb => {
                               if (sb.Length > 0) {
                                   sb.Remove(sb.Length - 2, 2);
                               }
                               return sb.ToString();
                           });
        }

        private string ProblemToString(ValidationProblem problem) {
            return problem.ToString();
        }
    }
}
